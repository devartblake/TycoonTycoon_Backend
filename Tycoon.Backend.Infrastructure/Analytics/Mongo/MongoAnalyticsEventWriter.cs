using MongoDB.Driver;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Models;

namespace Tycoon.Backend.Infrastructure.Analytics.Mongo
{
    public sealed class MongoAnalyticsEventWriter : IAnalyticsEventWriter
    {
        private readonly IMongoCollection<QuestionAnsweredAnalyticsEvent> _events;

        public MongoAnalyticsEventWriter(MongoClientFactory factory)
        {
            _events = factory.Database.GetCollection<QuestionAnsweredAnalyticsEvent>("question_answered_events");
        }

        public async Task UpsertQuestionAnsweredEventAsync(QuestionAnsweredAnalyticsEvent e, CancellationToken ct)
        {
            // Deterministic id -> idempotent upsert
            var filter = Builders<QuestionAnsweredAnalyticsEvent>.Filter.Eq(x => x.Id, e.Id);
            await _events.ReplaceOneAsync(filter, e, new ReplaceOptions { IsUpsert = true }, ct);
        }
    }
}
