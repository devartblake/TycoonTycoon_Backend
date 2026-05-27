using Mediator;
using Synaptix.Backend.Domain.Primitives;

namespace Synaptix.Backend.Domain.Notifications
{
    /// <summary>
    /// Mediator notification wrapper around a domain event.
    /// This avoids Application depending on Infrastructure types.
    /// </summary>
    public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
}
