using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Models;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.Backend.Infrastructure.Analytics.Writers
{
    /// <summary>
    /// EF Core-backed analytics event writer (PostgreSQL).
    /// Minimal implementation: persists the event as a new row.
    /// Avoids any "UpdateFrom" coupling to keep it stable and non-breaking.
    /// </summary>
    public sealed class PostgresAnalyticsEventWriter : IAnalyticsEventWriter
    {
        private readonly AppDb _db;

        public PostgresAnalyticsEventWriter(AppDb db)
        {
            _db = db;
        }
        public async Task UpsertQuestionAnsweredEventAsync(QuestionAnsweredAnalyticsEvent e, CancellationToken ct)
        {
            // Assumption: QuestionAnsweredAnalyticsEvent has a stable key (Id or composite uniqueness).
            // If you have a different key, adjust the lookup accordingly.
            //
            // Common patterns:
            // - e.Id (Guid)
            // - (e.PlayerId, e.QuestionId, e.OccurredAtUtc)
            //
            // Here we use Id if present; otherwise we fall back to Add().

            var set = _db.Set<QuestionAnsweredAnalyticsEvent>();

            // Try to upsert by Id if it exists.
            // If your model does not have Id, remove this and use a composite lookup.
            var idProp = typeof(QuestionAnsweredAnalyticsEvent).GetProperty("Id");

            if (idProp != null)
            {
                var idValue = idProp.GetValue(e);
                if (idValue != null)
                {
                    var existing = await set.FindAsync(new[] { idValue }, ct);
                    if (existing == null)
                    {
                        await set.AddAsync(e, ct);
                    }
                    else
                    {
                        // Copy current values onto tracked entity
                        _db.Entry(existing).CurrentValues.SetValues(e);
                    }

                    await _db.SaveChangesAsync(ct);
                    return;
                }
            }

            // If we cannot identify a key, persist as append-only
            await set.AddAsync(e, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task WriteQuestionAnsweredAsync(QuestionAnsweredAnalyticsEvent evt, CancellationToken ct = default)
        {
            // Uses EF Core Set<T>() so we do NOT require IAppDb to expose a DbSet property.
            _db.Set<QuestionAnsweredAnalyticsEvent>().Add(evt);
            await _db.SaveChangesAsync(ct);
        }
    }
}
