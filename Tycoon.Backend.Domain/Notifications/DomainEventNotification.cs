using MediatR;
using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Notifications
{
    /// <summary>
    /// MediatR notification wrapper around a domain event.
    /// This avoids Application depending on Infrastructure types.
    /// </summary>
    public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
}
