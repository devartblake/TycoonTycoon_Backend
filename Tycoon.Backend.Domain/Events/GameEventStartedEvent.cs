using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Events
{
    public sealed record GameEventStartedEvent(
        Guid GameEventId,
        string Kind
    ) : DomainEvent;
}
