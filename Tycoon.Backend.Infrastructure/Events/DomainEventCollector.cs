using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Infrastructure.Events
{
    internal static class DomainEventCollector
    {
        /// <summary>
        /// Collects and clears domain events from all tracked aggregate roots.
        /// </summary>
        public static IReadOnlyCollection<IDomainEvent> CollectAndClear(DbContext db)
        {
            var aggregates = db.ChangeTracker
                .Entries<AggregateRoot>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .Select(e => e.Entity)
                .ToList();

            var events = aggregates
                .SelectMany(a => a.DomainEvents)
                .ToList();

            foreach (var a in aggregates)
                a.ClearDomainEvents();

            return events;
        }
    }
}
