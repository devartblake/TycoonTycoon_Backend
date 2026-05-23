using Microsoft.AspNetCore.SignalR;
using Synaptix.Backend.Application.GameEvents;
using Synaptix.Backend.Api.Realtime.Clients;
using Synaptix.Shared.Contracts.Realtime.GameEvents;

namespace Synaptix.Backend.Api.Realtime
{
    public sealed class SignalRGameEventNotifier(IHubContext<NotificationHub, INotificationClient> hub)
        : IGameEventNotifier
    {
        public Task NotifyEliminationAsync(GameEventEliminationMessage message, CancellationToken ct)
            => hub.Clients.Group($"game-event:{message.GameEventId}").GameEventElimination(message);

        public Task NotifyEventClosedAsync(GameEventClosedMessage message, CancellationToken ct)
            => hub.Clients.Group($"game-event:{message.GameEventId}").GameEventClosed(message);
    }
}
