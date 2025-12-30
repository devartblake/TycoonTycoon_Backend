namespace Tycoon.Shared.Abstractions.Core.Domain.Events
{
    /// <summary>
    /// The domain events accessor interface.
    /// </summary>
    public interface IDomainEventsAccessor
    {
        IReadOnlyList<IDomainEvent> UnCommittedDomainEvents { get; }
    }
}
