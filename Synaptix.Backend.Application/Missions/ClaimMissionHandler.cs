using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Personalization;
using Synaptix.Backend.Application.Rewards;
using Synaptix.Backend.Domain.Entities; // Assuming MissionClaim is here
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Missions
{
    public sealed class ClaimMissionHandler : IRequestHandler<ClaimMission, ClaimMissionResult>
    {
        private readonly IAppDb _db;
        private readonly IPlayerMindProfileService? _mindProfiles;
        private readonly RewardOutcomeService? _rewardOutcomeService;
        private readonly HashSet<string> _reactorMissionAllowlist;
        private static readonly TimeSpan MissionReactorSpinTtl = TimeSpan.FromMinutes(5);
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public ClaimMissionHandler(
            IAppDb db,
            IPlayerMindProfileService? mindProfiles = null,
            RewardOutcomeService? rewardOutcomeService = null,
            IOptions<MissionRewardOptions>? rewardOptions = null)
        {
            _db = db;
            _mindProfiles = mindProfiles;
            _rewardOutcomeService = rewardOutcomeService;
            _reactorMissionAllowlist = rewardOptions?.Value.ReactorMissionKeys
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? [];
        }

        public async ValueTask<ClaimMissionResult> Handle(ClaimMission request, CancellationToken ct)
        {
            // Load claim (progress record)
            var claim = await _db.MissionClaims
                .SingleOrDefaultAsync(x => x.PlayerId == request.PlayerId && x.MissionId == request.MissionId, ct);

            // Fix CS8602: Handle null claim safely
            if (claim is null)
            {
                return CreateEmptyResult(request, ClaimMissionStatus.NotFound);
            }

            // Load mission definition
            var mission = await _db.Missions.FindAsync(new object[] { request.MissionId }, ct);

            // Fix CS8602: Handle null mission safely
            if (mission is null)
            {
                return CreateEmptyResult(request, ClaimMissionStatus.NotFound);
            }

            if (!claim.Completed || claim.Claimed)
            {
                var (mechanismId, existingReactorSpinPayload) = await GetExistingMissionRewardEnvelopeAsync(
                    request.PlayerId, request.MissionId, mission, ct);

                return new ClaimMissionResult(
                    Status: !claim.Completed ? ClaimMissionStatus.NotCompleted : ClaimMissionStatus.AlreadyClaimed,
                    PlayerId: request.PlayerId,
                    MissionId: request.MissionId,
                    MissionType: mission.Type,
                    MissionKey: mission.Key,
                    RewardXp: mission.RewardXp,
                    RewardCoins: mission.RewardCoins,
                    RewardDiamonds: mission.RewardDiamonds,
                    ClaimedAtUtcUtc: DateTime.UtcNow,
                    Completed: claim.Completed,
                    Claimed: claim.Claimed,
                    Progress: claim.Progress,
                    Goal: mission.Goal,
                    UpdatedMissions: await LoadMissionListAsync(request.PlayerId, request.TypeFilter, ct),
                    RewardMechanismId: mechanismId,
                    ReactorSpinPayload: existingReactorSpinPayload
                );
            }

            // Fix CS0200: Property 'Claimed' is read-only. 
            // Most domain-driven entities use a method to update state.
            // If 'MarkAsClaimed()' is not the correct name, check MissionClaim.cs for the setter method.
            claim.MarkClaimed();

            var (rewardMechanismId, reactorSpinPayload) = await CreateMissionRewardEnvelopeAsync(
                request.PlayerId, request.MissionId, mission, ct);

            await _db.SaveChangesAsync(ct);

            if (_mindProfiles is not null)
            {
                try
                {
                    await _mindProfiles.RecordEventAsync(request.PlayerId, new PlayerBehaviorEventDto(
                        EventType: "mission_completed",
                        EventSource: "mission",
                        Category: null,
                        Difficulty: null,
                        Mode: null,
                        Metadata: new Dictionary<string, object>
                        {
                            ["missionId"]   = request.MissionId,
                            ["missionType"] = mission.Type,
                            ["missionKey"]  = mission.Key,
                            ["rewardXp"]    = mission.RewardXp
                        },
                        OccurredAt: DateTimeOffset.UtcNow), ct);
                }
                catch { /* personalization must never break mission claiming */ }
            }

            var updatedMissions = await LoadMissionListAsync(request.PlayerId, request.TypeFilter, ct);

            return new ClaimMissionResult(
                Status: ClaimMissionStatus.Claimed,
                PlayerId: request.PlayerId,
                MissionId: request.MissionId,
                MissionType: mission.Type,
                MissionKey: mission.Key,
                RewardXp: mission.RewardXp,
                RewardCoins: mission.RewardCoins,
                RewardDiamonds: mission.RewardDiamonds,
                ClaimedAtUtcUtc: DateTime.UtcNow,
                Completed: claim.Completed,
                Claimed: claim.Claimed,
                Progress: claim.Progress,
                Goal: mission.Goal,
                UpdatedMissions: updatedMissions,
                RewardMechanismId: rewardMechanismId,
                ReactorSpinPayload: reactorSpinPayload
            );
        }

        private ClaimMissionResult CreateEmptyResult(ClaimMission request, ClaimMissionStatus status)
        {
            return new ClaimMissionResult(
                status, request.PlayerId, request.MissionId,
                "", "", 0, 0, 0, DateTime.UtcNow, false, false, 0, 0, Array.Empty<MissionListItem>(),
                null, null
            );
        }

        private async Task<(string? MechanismId, MissionReactorSpinPayload? ReactorSpinPayload)> CreateMissionRewardEnvelopeAsync(
            Guid playerId,
            Guid missionId,
            Mission mission,
            CancellationToken ct)
        {
            if (_rewardOutcomeService is null || !IsReactorMission(mission))
                return ("direct", null);

            var missionSessionIdempotencyKey = BuildMissionSpinIdempotencyKey(playerId, missionId);

            var existing = await _db.RewardSessions
                .FirstOrDefaultAsync(s =>
                    s.PlayerId == playerId &&
                    s.IdempotencyKey == missionSessionIdempotencyKey, ct);

            if (existing is not null)
                return ("reactor", BuildMissionReactorPayload(existing));

            var entry = _rewardOutcomeService.SelectFromPool(ReactorRewardPool.Entries);
            var (plainToken, tokenHash) = GenerateClaimToken();

            var policySnapshot = JsonSerializer.Serialize(new MissionRewardSnapshot(
                Source: "mission",
                ClaimToken: plainToken,
                MissionId: missionId));

            var session = RewardSession.Create(
                playerId,
                RewardMechanism.Reactor,
                entry.RewardId,
                JsonSerializer.Serialize(entry.Lines),
                JsonSerializer.Serialize(entry.Animation),
                missionSessionIdempotencyKey,
                tokenHash,
                DateTimeOffset.UtcNow + MissionReactorSpinTtl,
                policySnapshot,
                reactorId: "mission-reactor");

            _db.RewardSessions.Add(session);

            return ("reactor", BuildMissionReactorPayload(session, plainToken, entry));
        }

        private async Task<(string? MechanismId, MissionReactorSpinPayload? ReactorSpinPayload)> GetExistingMissionRewardEnvelopeAsync(
            Guid playerId,
            Guid missionId,
            Mission mission,
            CancellationToken ct)
        {
            if (_rewardOutcomeService is null || !IsReactorMission(mission))
                return (null, null);

            var existing = await _db.RewardSessions
                .FirstOrDefaultAsync(s =>
                    s.PlayerId == playerId &&
                    s.IdempotencyKey == BuildMissionSpinIdempotencyKey(playerId, missionId), ct);

            if (existing is null)
                return ("reactor", null);

            return ("reactor", BuildMissionReactorPayload(existing));
        }

        private static MissionReactorSpinPayload BuildMissionReactorPayload(
            RewardSession session,
            string? claimToken = null,
            RewardPoolEntry? entry = null)
        {
            var animation = JsonSerializer.Deserialize<RewardAnimationHint>(session.AnimationJson, JsonOpts)
                ?? new("three_reel_reactor", [], [], "common", "low");

            var lines = JsonSerializer.Deserialize<List<RewardLine>>(session.RewardLinesJson, JsonOpts) ?? [];

            var token = claimToken;
            if (string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(session.PolicySnapshotJson))
            {
                var snapshot = JsonSerializer.Deserialize<MissionRewardSnapshot>(session.PolicySnapshotJson, JsonOpts);
                token = snapshot?.ClaimToken;
            }

            return new MissionReactorSpinPayload(
                SpinId: session.SpinId,
                Status: session.Status.ToString(),
                ExpiresAtUtc: session.ExpiresAtUtc,
                CooldownUntilUtc: null,
                Animation: new MissionReactorAnimationPayload(
                    animation.Layout,
                    animation.Symbols,
                    animation.WinningSymbolIndexes,
                    animation.Rarity,
                    animation.Intensity),
                RewardPreview: new MissionReactorRewardPreviewPayload(
                    session.RewardId,
                    entry?.DisplayName ?? session.RewardId,
                    lines.Select(l => new MissionReactorRewardLinePayload(l.Type, l.Amount)).ToList()),
                ClaimToken: token ?? string.Empty
            );
        }

        private bool IsReactorMission(Mission mission)
            => _reactorMissionAllowlist.Contains(mission.Key);

        private static string BuildMissionSpinIdempotencyKey(Guid playerId, Guid missionId)
            => $"mission:reactor:{playerId:N}:{missionId:N}";

        private static (string Token, string Hash) GenerateClaimToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(bytes);
            var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
            return (token, hash);
        }

        private sealed record MissionRewardSnapshot(string Source, string ClaimToken, Guid MissionId);

        private async Task<IReadOnlyList<MissionListItem>> LoadMissionListAsync(Guid playerId, string typeFilter, CancellationToken ct)
        {
            var missionsQuery = _db.Missions.AsNoTracking().Where(m => m.Active);
            if (!string.IsNullOrWhiteSpace(typeFilter))
                missionsQuery = missionsQuery.Where(m => m.Type == typeFilter);

            var missions = await missionsQuery.ToListAsync(ct);
            var claims = await _db.MissionClaims.AsNoTracking()
                .Where(c => c.PlayerId == playerId)
                .ToListAsync(ct);

            var byMissionId = claims.ToDictionary(x => x.MissionId, x => x);

            return missions.Select(m =>
            {
                byMissionId.TryGetValue(m.Id, out var c);
                return new MissionListItem(
                    MissionId: m.Id,
                    Type: m.Type,
                    Key: m.Key,
                    Goal: m.Goal,
                    Active: m.Active,
                    Progress: c?.Progress ?? 0,
                    Completed: c?.Completed ?? false,
                    Claimed: c?.Claimed ?? false // Safe navigation used here
                );
            }).ToList();
        }
    }
}