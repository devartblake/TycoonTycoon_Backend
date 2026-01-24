using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Tycoon.Backend.Application.Analytics;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Models;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Application.Abstractions;

namespace Tycoon.Backend.Application.Missions.Jobs
{
    public sealed class QuestionAnsweredMissionJob
    {
        private readonly IAppDb _db;
        private readonly MissionProgressService _missions;
        private readonly IAnalyticsEventWriter _eventWriter;
        private readonly IRollupStore _rollups;
        private readonly IRollupIndexer? _indexer;

        public QuestionAnsweredMissionJob(
            IAppDb db,
            MissionProgressService missions,
            IAnalyticsEventWriter eventWriter,
            IRollupStore rollups,
            IRollupIndexer? indexer = null)
        {
            _db = db;
            _missions = missions;
            _eventWriter = eventWriter;
            _rollups = rollups;
            _indexer = indexer;
        }

        public async Task RunAsync(
            Guid matchId,
            Guid playerId,
            string mode,
            string category,
            int difficulty,
            bool isCorrect,
            int answerTimeMs,
            CancellationToken ct = default)
        {
            var missions = await _db.Missions.AsNoTracking()
                .Where(m => m.Active)
                .ToListAsync(ct);

            var claims = await _db.MissionClaims
                .Where(c => c.PlayerId == playerId)
                .ToListAsync(ct);

            foreach (var m in missions)
            {
                if (m.Type == "Daily" && m.Key == "daily_answer_20")
                {
                    await AddProgressSafeAsync(playerId, m, claims, amount: 1, ct);
                }
            }

            var answeredAtUtc = DateTime.UtcNow; // or pass from event if available
            var eventId = AnalyticsIds.QuestionAnsweredEventId(
                matchId, playerId, answeredAtUtc, mode, category, difficulty, isCorrect, answerTimeMs);

            var evt = new QuestionAnsweredAnalyticsEvent(
                id: eventId,
                matchId: matchId,
                playerId: playerId,
                mode: mode,
                category: category,
                difficulty: difficulty,
                isCorrect: isCorrect,
                answerTimeMs: answerTimeMs,
                answeredAtUtc: answeredAtUtc
            );

            await _eventWriter.UpsertQuestionAnsweredEventAsync(evt, ct);

            // Rollup
            var date = DateOnly.FromDateTime(answeredAtUtc);
            var daily = await _rollups.UpsertDailyRollupAsync(
                date, mode, category, difficulty, isCorrect, answerTimeMs, answeredAtUtc, ct);
            var playerDaily = await _rollups.UpsertPlayerDailyRollupAsync(
                date, playerId, mode, category, difficulty, isCorrect, answerTimeMs, answeredAtUtc, ct);
            // Update aggregate daily rollup
            var rollup = await _rollups.UpsertDailyRollupAsync(
                utcDate: date,
                mode: mode,
                category: category,
                difficulty: difficulty,
                isCorrect: isCorrect,
                answerTimeMs: answerTimeMs,
                answeredAtUtc: answeredAtUtc,
                ct: ct
            );

            // Index rollup into Elastic (idempotent)
            if (_indexer is not null)
            {
                await _indexer.IndexDailyRollupAsync(rollup, ct);
                await _indexer.IndexDailyRollupAsync(daily, ct);
                await _indexer.IndexPlayerDailyRollupAsync(playerDaily, ct);
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
            var claim = localClaims.FirstOrDefault(x => x.MissionId == mission.Id);
            if (claim is null)
            {
                claim = new MissionClaim(playerId, mission.Id);
                _db.MissionClaims.Add(claim);

                try
                {
                    await _db.SaveChangesAsync(ct);
                    localClaims.Add(claim);
                }
                catch (DbUpdateException)
                {
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
