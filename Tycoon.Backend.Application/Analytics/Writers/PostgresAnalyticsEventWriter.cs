using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Models;
using Tycoon.Backend.Application.Abstractions;

namespace Tycoon.Backend.Application.Analytics.Writers
{
    public sealed class PostgresAnalyticsEventWriter : IAnalyticsEventWriter
    {
        private readonly IAppDb _db;

        public PostgresAnalyticsEventWriter(IAppDb db)
        {
            _db = db;
        }

        public async Task UpsertQuestionAnsweredEventAsync(
            QuestionAnsweredAnalyticsEvent evt,
            CancellationToken ct = default)
        {
            var existing = await _db.QuestionAnsweredAnalyticsEvents
                .FindAsync(new object[] { evt.Id }, ct);

            if (existing is null)
            {
                _db.QuestionAnsweredAnalyticsEvents.Add(evt);
            }
            else
            {
                existing.UpdateFrom(evt);
            }
        }
    }
}
