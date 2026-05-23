using Synaptix.Backend.Domain.Primitives;

namespace Synaptix.Backend.Domain.Events
{
    public sealed record GameEventStartedEvent(
        Guid GameEventId,
        string Kind
    ) : DomainEvent;
}
