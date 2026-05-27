using Mediator;

namespace Synaptix.MigrationService;

internal sealed class NoOpDomainEventPublisher : IPublisher
{
    public ValueTask Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask Publish(
        object notification,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
