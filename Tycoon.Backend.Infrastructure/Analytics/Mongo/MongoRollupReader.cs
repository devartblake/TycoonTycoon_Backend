using MongoDB.Driver;
using Tycoon.Backend.Application.Analytics.Models;

namespace Tycoon.Backend.Infrastructure.Analytics.Mongo
{
    /// <summary>
    /// Streams rollup documents from Mongo for rebuild/reindex operations.
    /// </summary>
    public sealed class MongoRollupReader
    {
        private readonly IMongoCollection<QuestionAnsweredDailyRollup> _daily;
        private readonly IMongoCollection<QuestionAnsweredPlayerDailyRollup> _playerDaily;

        public MongoRollupReader(MongoClientFactory factory)
        {
            _daily = factory.Database.GetCollection<QuestionAnsweredDailyRollup>("qa_daily_rollups");
            _playerDaily = factory.Database.GetCollection<QuestionAnsweredPlayerDailyRollup>("qa_player_daily_rollups");
        }

        public async IAsyncEnumerable<QuestionAnsweredDailyRollup> ReadDailyAsync(
            DateOnly? fromUtcDate,
            DateOnly? toUtcDate,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            var filter = BuildUtcDateFilter<QuestionAnsweredDailyRollup>(x => x.UtcDate, fromUtcDate, toUtcDate);

            using var cursor = await _daily.Find(filter).ToCursorAsync(ct);
            while (await cursor.MoveNextAsync(ct))
            {
                foreach (var doc in cursor.Current)
                    yield return doc;
            }
        }

        public async IAsyncEnumerable<QuestionAnsweredPlayerDailyRollup> ReadPlayerDailyAsync(
            DateOnly? fromUtcDate,
            DateOnly? toUtcDate,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            var filter = BuildUtcDateFilter<QuestionAnsweredPlayerDailyRollup>(x => x.UtcDate, fromUtcDate, toUtcDate);

            using var cursor = await _playerDaily.Find(filter).ToCursorAsync(ct);
            while (await cursor.MoveNextAsync(ct))
            {
                foreach (var doc in cursor.Current)
                    yield return doc;
            }
        }

        private static FilterDefinition<T> BuildUtcDateFilter<T>(
            System.Linq.Expressions.Expression<Func<T, DateOnly>> dateField,
            DateOnly? fromUtcDate,
            DateOnly? toUtcDate)
        {
            var f = Builders<T>.Filter;

            if (fromUtcDate is null && toUtcDate is null)
                return f.Empty;

            if (fromUtcDate is not null && toUtcDate is not null)
                return f.Gte(dateField, fromUtcDate.Value) & f.Lte(dateField, toUtcDate.Value);

            if (fromUtcDate is not null)
                return f.Gte(dateField, fromUtcDate.Value);

            return f.Lte(dateField, toUtcDate!.Value);
        }
    }
}
