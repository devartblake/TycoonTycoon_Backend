using Mediator;
using Microsoft.Extensions.Logging;
using Synaptix.Backend.Domain.Abstractions;
using Synaptix.Backend.Domain.Notifications;
using Synaptix.Backend.Domain.Primitives;

namespace Synaptix.Backend.Infrastructure.Events
{
    public sealed class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IPublisher _publisher;
        private readonly ILogger<DomainEventDispatcher> _logger;

        public DomainEventDispatcher(IPublisher publisher, ILogger<DomainEventDispatcher> logger)
        {
            _publisher = publisher;
            _logger = logger;
        }

        public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct)
        {
            foreach (var evt in events)
            {
                try
                { 
                    await _publisher.Publish(new DomainEventNotification(evt), ct); 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Domain event dispatch failed for {EventType}", evt.GetType().Name);
                    throw;
                }
            }
        }
    }
}
