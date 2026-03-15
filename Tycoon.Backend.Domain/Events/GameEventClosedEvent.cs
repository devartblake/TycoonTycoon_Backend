using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Events
{
    public sealed record GameEventClosedEvent(
        Guid GameEventId,
        string Kind,
        int TotalParticipants,
        int JackpotPool
    ) : DomainEvent;
}
