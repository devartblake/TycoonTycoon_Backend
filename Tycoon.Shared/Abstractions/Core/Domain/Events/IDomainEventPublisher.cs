namespace Tycoon.Shared.Abstractions.Core.Domain.Events
{
    /// <summary>
    /// The domain event publisher interface.
        /// </summary>
    public interface IDomainEventPublisher
    {
        Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
        Task PublishAsync(IDomainEvent[] domainEvents, CancellationToken cancellationToken = default);
    }
}
