using Synaptix.Backend.Domain.Primitives;

namespace Synaptix.Backend.Domain.Events
{
    public sealed record GameEventOpenedEvent(
        Guid GameEventId,
        string Kind,
        int TierId,
        DateTimeOffset ScheduledAtUtc
    ) : DomainEvent;
}
