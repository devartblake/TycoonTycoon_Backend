using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;

namespace Tycoon.Backend.Application.Missions
{
    public sealed class ClaimMissionHandler : IRequestHandler<ClaimMission, ClaimMissionResult>
    {
        private readonly IAppDb _db;

        public ClaimMissionHandler(IAppDb db)
        {
            _db = db;
        }

        public async Task<ClaimMissionResult> Handle(ClaimMission request, CancellationToken ct)
        {
            // Load claim (progress record)
            var claim = await _db.MissionClaims
                .SingleOrDefaultAsync(x => x.PlayerId == request.PlayerId && x.MissionId == request.MissionId, ct);

            if (claim is null)
            {
                return new ClaimMissionResult(
                    Status: ClaimMissionStatus.NotFound,
                    PlayerId: request.PlayerId,
                    MissionId: request.MissionId,
                    MissionType: "",
                    MissionKey: "",
                    RewardXp: 0,
                    RewardCoins: 0,
                    RewardDiamonds: 0,
                    ClaimedAtUtcUtc: DateTime.UtcNow,
                    Completed: false,
                    Claimed: false,
                    Progress: 0,
                    Goal: 0,
                    UpdatedMissions: Array.Empty<MissionListItem>()
                );
            }

            // Load mission definition (for Goal)
            var mission = await _db.Missions.SingleOrDefaultAsync(m => m.Id == request.MissionId, ct);
            if (mission is null)
            {
                return new ClaimMissionResult(
                    Status: ClaimMissionStatus.NotFound,
                    PlayerId: request.PlayerId,
                    MissionId: request.MissionId,
                    MissionType: "",
                    MissionKey: "",
                    RewardXp: 0,
                    RewardCoins: 0,
                    RewardDiamonds: 0,
                    ClaimedAtUtcUtc: DateTime.UtcNow,
                    Completed: claim.Completed,
                    Claimed: claim.Claimed,
                    Progress: claim.Progress,
                    Goal: 0,
                    UpdatedMissions: Array.Empty<MissionListItem>()
                );
            }

            var goal = mission?.Goal ?? 0;

            // Not completed yet
            if (!claim.Completed)
            {
                var updated = await LoadMissionListAsync(request.PlayerId, request.TypeFilter, ct);
                return new ClaimMissionResult(
                    Status: ClaimMissionStatus.NotCompleted,
                    PlayerId: claim.PlayerId,
                    MissionId: claim.MissionId,
                    MissionType: mission.Type,
                    MissionKey: mission.Key,
                    RewardXp: 0,
                    RewardCoins: 0,
                    RewardDiamonds: 0,
                    ClaimedAtUtcUtc: DateTime.UtcNow,
                    Completed: claim.Completed,
                    Claimed: claim.Claimed,
                    Progress: claim.Progress,
                    Goal: mission.Goal,
                    UpdatedMissions: updated
                );
            }

            // Already claimed (idempotent)
            if (claim.Claimed)
            {
                var updated = await LoadMissionListAsync(request.PlayerId, request.TypeFilter, ct);
                return new ClaimMissionResult(
                    Status: ClaimMissionStatus.AlreadyClaimed,
                    PlayerId: claim.PlayerId,
                    MissionId: claim.MissionId,
                    MissionType: mission.Type,
                    MissionKey: mission.Key,
                    RewardXp: mission.RewardXp,
                    RewardCoins: mission.RewardCoins,
                    RewardDiamonds: mission.RewardDiamonds,
                    ClaimedAtUtcUtc: claim.ClaimedAtUtc ?? DateTime.UtcNow,
                    Completed: claim.Completed,
                    Claimed: claim.Claimed,
                    Progress: claim.Progress,
                    Goal: mission.Goal,
                    UpdatedMissions: updated
                );
            }

            // Transaction: mark claimed + grant reward
            var now = DateTime.UtcNow;

            // 1) Mark claimed
            // Mark claimed (domain method if you have it; otherwise set fields)
            // Prefer: claim.MarkClaimed();
            claim.MarkClaimed();
            //claim.ClaimedAtUtc = now;

            // 2) Award reward (Player fields)
            var player = await _db.Players.SingleOrDefaultAsync(p => p.Id == request.PlayerId, ct);
            if (player is not null)
            {
                // Adjust these field names to your Player model if needed
                player.AddXp(mission.RewardXp);
                player.AddCoins(mission.RewardCoins);
                player.AddDiamonds(mission.RewardDiamonds);
            }

            await _db.SaveChangesAsync(ct);

            // Reward granting (XP/coins) should happen here later once your mission schema includes rewards.
            // Example later:
            // player.AddXp(mission.RewardXp); wallet.AddCoins(mission.RewardCoins);

            // 3) Return updated mission list
            var updatedMissions = await LoadMissionListAsync(request.PlayerId, request.TypeFilter, ct);

            return new ClaimMissionResult(
                Status: ClaimMissionStatus.Claimed,
                PlayerId: claim.PlayerId,
                MissionId: claim.MissionId,
                MissionType: mission.Type,
                MissionKey: mission.Key,
                RewardXp: mission.RewardXp,
                RewardCoins: mission.RewardCoins,
                RewardDiamonds: mission.RewardDiamonds,
                ClaimedAtUtcUtc: now,
                Completed: claim.Completed,
                Claimed: claim.Claimed,
                Progress: claim.Progress,
                Goal: mission.Goal,
                UpdatedMissions: updatedMissions
            );
        }

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
                    Claimed: c?.Claimed ?? false
                );
            }).ToList();
        }
    }
}
