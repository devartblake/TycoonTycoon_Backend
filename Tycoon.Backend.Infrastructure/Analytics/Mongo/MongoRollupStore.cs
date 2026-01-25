using MongoDB.Driver;
using Tycoon.Backend.Application.Analytics;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Models;

namespace Tycoon.Backend.Infrastructure.Analytics.Mongo
{
    public sealed class MongoRollupStore : IRollupStore
    {
        private readonly IMongoCollection<QuestionAnsweredDailyRollup> _daily;
        private readonly IMongoCollection<QuestionAnsweredPlayerDailyRollup> _playerDaily;

        public MongoRollupStore(MongoClientFactory factory)
        {
            _daily = factory.Database.GetCollection<QuestionAnsweredDailyRollup>("qa_daily_rollups");
            _playerDaily = factory.Database.GetCollection<QuestionAnsweredPlayerDailyRollup>("qa_player_daily_rollups");
        }

        public async Task<QuestionAnsweredDailyRollup> UpsertDailyRollupAsync(
            DateOnly day,
            string mode,
            string category,
            int difficulty,
            bool isCorrect,
            int answerTimeMs,
            DateTime answeredAtUtc,
            CancellationToken ct)
        {
            var id = AnalyticsIds.DailyRollupId(day, mode, category, difficulty);

            // Atomic update pipeline
            var filter = Builders<QuestionAnsweredDailyRollup>.Filter.Eq(x => x.Id, id);

            var update = Builders<QuestionAnsweredDailyRollup>.Update
                .SetOnInsert(x => x.Id, id)
                .SetOnInsert(x => x.Day, day)
                .SetOnInsert(x => x.Mode, mode)
                .SetOnInsert(x => x.Category, category)
                .SetOnInsert(x => x.Difficulty, difficulty)
                .Inc(x => x.TotalAnswers, 1)
                .Inc(x => x.CorrectAnswers, isCorrect ? 1 : 0)
                .Inc(x => x.WrongAnswers, isCorrect ? 0 : 1)
                .Inc(x => x.SumAnswerTimeMs, answerTimeMs)
                .Set(x => x.UpdatedAtUtc, answeredAtUtc);

            // Min/max need special handling: we do a best-effort approach.
            // If document doesn't exist, set both to answerTimeMs; otherwise update if better.
            var existing = await _daily.Find(filter).FirstOrDefaultAsync(ct);
            if (existing is null)
            {
                update = update.Set(x => x.MinAnswerTimeMs, answerTimeMs)
                               .Set(x => x.MaxAnswerTimeMs, answerTimeMs);
            }
            else
            {
                var min = existing.MinAnswerTimeMs == 0 ? answerTimeMs : Math.Min(existing.MinAnswerTimeMs, answerTimeMs);
                var max = Math.Max(existing.MaxAnswerTimeMs, answerTimeMs);
                update = update.Set(x => x.MinAnswerTimeMs, min)
                               .Set(x => x.MaxAnswerTimeMs, max);
            }

            await _daily.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);

            // Return the updated rollup
            return await _daily.Find(filter).FirstAsync(ct);
        }

        public async Task<QuestionAnsweredPlayerDailyRollup> UpsertPlayerDailyRollupAsync(
            DateOnly day,
            Guid playerId,
            string mode,
            string category,
            int difficulty,
            bool isCorrect,
            int answerTimeMs,
            DateTime answeredAtUtc,
            CancellationToken ct)
        {
            var id = AnalyticsIds.PlayerDailyRollupId(day, playerId, mode, category, difficulty);

            var filter = Builders<QuestionAnsweredPlayerDailyRollup>.Filter.Eq(x => x.Id, id);

            var update = Builders<QuestionAnsweredPlayerDailyRollup>.Update
                .SetOnInsert(x => x.Id, id)
                .SetOnInsert(x => x.Day, day)
                .SetOnInsert(x => x.PlayerId, playerId)
                .SetOnInsert(x => x.Mode, mode)
                .SetOnInsert(x => x.Category, category)
                .SetOnInsert(x => x.Difficulty, difficulty)
                .Inc(x => x.TotalAnswers, 1)
                .Inc(x => x.CorrectAnswers, isCorrect ? 1 : 0)
                .Inc(x => x.WrongAnswers, isCorrect ? 0 : 1)
                .Inc(x => x.SumAnswerTimeMs, answerTimeMs)
                .Set(x => x.UpdatedAtUtc, answeredAtUtc);

            var existing = await _playerDaily.Find(filter).FirstOrDefaultAsync(ct);
            if (existing is null)
            {
                update = update.Set(x => x.MinAnswerTimeMs, answerTimeMs)
                               .Set(x => x.MaxAnswerTimeMs, answerTimeMs);
            }
            else
            {
                var min = existing.MinAnswerTimeMs == 0 ? answerTimeMs : Math.Min(existing.MinAnswerTimeMs, answerTimeMs);
                var max = Math.Max(existing.MaxAnswerTimeMs, answerTimeMs);
                update = update.Set(x => x.MinAnswerTimeMs, min)
                               .Set(x => x.MaxAnswerTimeMs, max);
            }

            await _playerDaily.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);
            return await _playerDaily.Find(filter).FirstAsync(ct);
        }

    }
}
