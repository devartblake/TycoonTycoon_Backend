namespace Tycoon.Backend.Domain.Primitives
{
    /// <summary>
    /// Marker interface for domain events.
    /// Domain events are internal to the domain and should not be transported directly.
    /// Map to integration events in Application/Infrastructure if needed.
    /// </summary>
    public interface IDomainEvent
    {
        Guid EventId { get; }
        DateTime OccurredAtUtc { get; }
    }
}
