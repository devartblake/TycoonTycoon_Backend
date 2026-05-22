using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Personalization;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Missions
{
    /// <summary>
    /// Updates mission progress for a player based on gameplay signals.
    /// MissionClaim is the progress record (PlayerId + MissionId must be unique).
    /// </summary>
    public sealed class MissionProgressService
    {
        private readonly IAppDb _db;
        private readonly IPlayerMindProfileService? _mindProfiles;

        public MissionProgressService(IAppDb db, IPlayerMindProfileService? mindProfiles = null)
        {
            _db = db;
            _mindProfiles = mindProfiles;
        }

        public async Task ApplyMatchCompletedAsync(
            Guid playerId,
            bool isWin,
            int correctAnswers,
            int totalQuestions,
            int durationSeconds,
            CancellationToken ct)
        {
            var missions = await _db.Missions.AsNoTracking()
                .Where(m => m.Active)
                .ToListAsync(ct);

            var claims = await _db.MissionClaims
                .Where(c => c.PlayerId == playerId)
                .ToListAsync(ct);

            foreach (var m in missions)
            {
                // Minimal examples (keep your keys; expand later)
                if (m.Type == "Daily" && m.Key == "daily_play_3")
                {
                    await AddProgressSafeAsync(playerId, m, claims, amount: 1, ct);
                }
                else if (m.Type == "Daily" && m.Key == "daily_win_1" && isWin)
                {
                    await AddProgressSafeAsync(playerId, m, claims, amount: 1, ct);
                }
                else if (m.Type == "Weekly" && m.Key == "weekly_win_10" && isWin)
                {
                    await AddProgressSafeAsync(playerId, m, claims, amount: 1, ct);
                }
                else if (m.Type == "Weekly" && m.Key == "weekly_play_25")
                {
                    await AddProgressSafeAsync(playerId, m, claims, amount: 1, ct);
                }
            }

            await _db.SaveChangesAsync(ct);

            if (_mindProfiles is not null)
            {
                try
                {
                    await _mindProfiles.RecordEventAsync(playerId, new PlayerBehaviorEventDto(
                        EventType: "match_completed",
                        EventSource: "match",
                        Category: null,
                        Difficulty: null,
                        Mode: null,
                        Metadata: new Dictionary<string, object>
                        {
                            ["isWin"] = isWin,
                            ["correctAnswers"] = correctAnswers,
                            ["totalQuestions"] = totalQuestions,
                            ["durationSeconds"] = durationSeconds
                        },
                        OccurredAt: DateTimeOffset.UtcNow), ct);
                }
                catch { /* personalization must never break match completion */ }
            }
        }

        public async Task ApplyGameEventCompletedAsync(
            Guid playerId,
            string eventKind,
            int rank,
            int survivorsEliminated,
            bool becameGuardian,
            bool defendedGuardian,
            bool capturedTerritory,
            int ownedTileCount,
            CancellationToken ct)
        {
            var missions = await _db.Missions.AsNoTracking()
                .Where(m => m.Active)
                .ToListAsync(ct);

            var claims = await _db.MissionClaims
                .Where(c => c.PlayerId == playerId)
                .ToListAsync(ct);

            foreach (var m in missions)
            {
                if (m.Key == "event_enter_1")
                    await AddProgressSafeAsync(playerId, m, claims, 1, ct);
                else if (m.Key == "event_top20_1" && rank > 0 && rank <= 20)
                    await AddProgressSafeAsync(playerId, m, claims, 1, ct);
                else if (m.Key == "event_win_1" && rank == 1)
                    await AddProgressSafeAsync(playerId, m, claims, 1, ct);
                else if (m.Key == "event_survive_50" && survivorsEliminated > 0)
                    await AddProgressSafeAsync(playerId, m, claims, survivorsEliminated, ct);
                else if (m.Key == "guardian_become_1" && becameGuardian)
                    await AddProgressSafeAsync(playerId, m, claims, 1, ct);
                else if (m.Key == "guardian_defend_3" && defendedGuardian)
                    await AddProgressSafeAsync(playerId, m, claims, 1, ct);
                else if (m.Key == "territory_capture_1" && capturedTerritory)
                    await AddProgressSafeAsync(playerId, m, claims, 1, ct);
                else if (m.Key == "territory_own_5" && ownedTileCount >= 5)
                    await AddProgressSafeAsync(playerId, m, claims, 1, ct);
                else if (m.Key == "champion_eliminate_10" && eventKind == "champion_battle" && survivorsEliminated > 0)
                    await AddProgressSafeAsync(playerId, m, claims, survivorsEliminated, ct);
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task ApplyRoundCompletedAsync(
            Guid playerId,
            bool perfectRound,
            int avgAnswerTimeMs,
            CancellationToken ct)
        {
            var missions = await _db.Missions.AsNoTracking()
                .Where(m => m.Active)
                .ToListAsync(ct);

            var claims = await _db.MissionClaims
                .Where(c => c.PlayerId == playerId)
                .ToListAsync(ct);

            foreach (var m in missions)
            {
                if (m.Type == "Weekly" && m.Key == "weekly_perfect_5" && perfectRound)
                {
                    await AddProgressSafeAsync(playerId, m, claims, amount: 1, ct);
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        private async Task AddProgressSafeAsync(
            Guid playerId,
            Mission mission,
            List<MissionClaim> localClaims,
            int amount,
            CancellationToken ct)
        {
            // Try local first (already loaded)
            var claim = localClaims.FirstOrDefault(x => x.MissionId == mission.Id);
            if (claim is null)
            {
                claim = new MissionClaim(playerId, mission.Id);
                _db.MissionClaims.Add(claim);

                try
                {
                    // Flush immediately so unique constraint races are handled here
                    await _db.SaveChangesAsync(ct);
                    localClaims.Add(claim);
                }
                catch (DbUpdateException)
                {
                    // Another concurrent request inserted it; reload
                    _db.Entry(claim).State = EntityState.Detached;

                    claim = await _db.MissionClaims
                        .SingleAsync(x => x.PlayerId == playerId && x.MissionId == mission.Id, ct);

                    localClaims.Add(claim);
                }
            }

            claim.AddProgress(amount, mission.Goal);
        }
    }
}
