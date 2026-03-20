using Microsoft.AspNetCore.SignalR;
using Tycoon.Backend.Application.GameEvents;
using Tycoon.Backend.Api.Realtime.Clients;
using Tycoon.Shared.Contracts.Realtime.GameEvents;

namespace Tycoon.Backend.Api.Realtime
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
