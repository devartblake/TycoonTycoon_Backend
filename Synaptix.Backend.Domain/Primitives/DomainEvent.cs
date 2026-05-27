using Mediator;

namespace Synaptix.Backend.Domain.Primitives
{
    /// <summary>
    /// Base record for domain events.
    /// Implements the SHARED IDomainEvent (satisfies IDomainEventHandler constraint)
    /// and Mediator.INotification (satisfies INotificationHandler / IEventHandler constraint).
    /// The local Synaptix.Backend.Domain.Primitives.IDomainEvent marker is kept separately
    /// for internal domain use but is NOT what the handler pipeline checks against.
    /// </summary>
    public abstract record DomainEvent
        : IDomainEvent, // shared — what IDomainEventHandler<TEvent> checks
          INotification                                                 // Mediator (source-gen) — what INotificationHandler checks
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }
}