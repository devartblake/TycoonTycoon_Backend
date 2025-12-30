using MediatR;

namespace Tycoon.Backend.Domain.Primitives
{
    /// <summary>
    /// Base record for domain events.
    /// Implements both IDomainEvent (domain pipeline) and INotification (MediatR).
    /// </summary>
    public abstract record DomainEvent : IDomainEvent, INotification
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }
}
