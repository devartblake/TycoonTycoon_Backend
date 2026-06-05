using MongoDB.Driver;
using Synaptix.Backend.Application.Analytics.Abstractions;
using Synaptix.Backend.Application.Analytics.Models;

namespace Synaptix.Backend.Infrastructure.Analytics.Mongo
{
    public sealed class MongoAnalyticsEventWriter : IAnalyticsEventWriter
    {
        private readonly IMongoCollection<QuestionAnsweredAnalyticsEvent> _events;

        public MongoAnalyticsEventWriter(MongoClientFactory factory)
        {
            _events = factory.Database.GetCollection<QuestionAnsweredAnalyticsEvent>("question_answered_events");
        }

        public async Task<bool> UpsertQuestionAnsweredEventAsync(QuestionAnsweredAnalyticsEvent e, CancellationToken ct)
        {
            // Deterministic id -> idempotent upsert
            var filter = Builders<QuestionAnsweredAnalyticsEvent>.Filter.Eq(x => x.Id, e.Id);
            var result = await _events.ReplaceOneAsync(filter, e, new ReplaceOptions { IsUpsert = true }, ct);
            return result.UpsertedId is not null;
        }
    }
}
