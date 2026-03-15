using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Events
{
    public sealed record GameEventOpenedEvent(
        Guid GameEventId,
        string Kind,
        int TierId,
        DateTimeOffset ScheduledAtUtc
    ) : DomainEvent;
}
