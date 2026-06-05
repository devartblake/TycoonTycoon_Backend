using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Synaptix.Backend.Application.Analytics;
using Synaptix.Backend.Application.Analytics.Models;
using Synaptix.Backend.Application.Personalization;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Missions.Jobs
{
    public sealed class QuestionAnsweredMissionJob
    {
        private readonly IAppDb _db;
        private readonly MissionProgressService _missions;
        private readonly QuestionAnsweredAnalyticsPersistence _analytics;
        private readonly IPlayerMindProfileService? _mindProfiles;

        public QuestionAnsweredMissionJob(
            IAppDb db,
            MissionProgressService missions,
            QuestionAnsweredAnalyticsPersistence analytics,
            IPlayerMindProfileService? mindProfiles = null)
        {
            _db = db;
            _missions = missions;
            _analytics = analytics;
            _mindProfiles = mindProfiles;
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

            await _analytics.PersistAsync(evt, ct);

            await _db.SaveChangesAsync(ct);

            if (_mindProfiles is not null)
            {
                try
                {
                    await _mindProfiles.RecordEventAsync(playerId, new PlayerBehaviorEventDto(
                        EventType: "question_answered",
                        EventSource: "match",
                        Category: category,
                        Difficulty: difficulty.ToString(),
                        Mode: mode,
                        Metadata: new Dictionary<string, object>
                        {
                            ["correct"] = isCorrect,
                            ["answerTimeMs"] = answerTimeMs
                        },
                        OccurredAt: DateTimeOffset.UtcNow), ct);
                }
                catch { /* personalization must never break gameplay */ }
            }
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
