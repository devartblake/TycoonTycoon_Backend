using Synaptix.Backend.Domain.Primitives;

namespace Synaptix.Backend.Domain.Events
{
    public sealed record GameEventClosedEvent(
        Guid GameEventId,
        string Kind,
        int TotalParticipants,
        int JackpotPool
    ) : DomainEvent;
}
