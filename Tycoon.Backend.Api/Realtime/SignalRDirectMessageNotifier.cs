using Microsoft.AspNetCore.SignalR;
using Tycoon.Backend.Api.Realtime.Clients;
using Tycoon.Backend.Application.Realtime;
using Tycoon.Shared.Contracts.Realtime.Notifications;

namespace Tycoon.Backend.Api.Realtime
{
    public sealed class SignalRDirectMessageNotifier(IHubContext<NotificationHub, INotificationClient> hub)
        : IDirectMessageNotifier
    {
        public Task NotifyDirectMessagesUpdatedAsync(Guid playerId, Guid? conversationId, int unreadCount, string reason, CancellationToken ct)
            => hub.Clients.Group($"player:{playerId}")
                .DirectMessagesUpdated(new DirectMessagesUpdatedMessage(playerId, conversationId, unreadCount, reason, DateTimeOffset.UtcNow));
    }
}
