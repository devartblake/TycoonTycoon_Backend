using Mediator;

namespace Tycoon.Shared.Abstractions.Core.Domain.Events {
    /// <summary>
    /// The event handler interface.
    /// </summary>
    public interface IEventHandler<in TEvent> : INotificationHandler<TEvent>
    where TEvent : INotification;
}
