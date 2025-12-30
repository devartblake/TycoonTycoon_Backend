using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Application.Abstractions;

namespace Tycoon.Backend.Application.Missions
{
    /// <summary>
    /// Updates mission progress for a player based on gameplay signals.
    /// MissionClaim is the progress record (PlayerId + MissionId must be unique).
    /// </summary>
    public sealed class MissionProgressService
    {
        private readonly IAppDb _db;

        public MissionProgressService(IAppDb db)
        {
            _db = db;
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
