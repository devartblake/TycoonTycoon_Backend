using Microsoft.AspNetCore.SignalR;
using Synaptix.Backend.Api.Realtime.Clients;
using Synaptix.Backend.Application.Realtime;
using Synaptix.Shared.Contracts.Realtime.Notifications;

namespace Synaptix.Backend.Api.Realtime
{
    public sealed class SignalRDirectMessageNotifier(IHubContext<NotificationHub, INotificationClient> hub)
        : IDirectMessageNotifier
    {
        public Task NotifyDirectMessagesUpdatedAsync(Guid playerId, Guid? conversationId, int unreadCount, string reason, CancellationToken ct)
            => hub.Clients.Group($"player:{playerId}")
                .DirectMessagesUpdated(new DirectMessagesUpdatedMessage(playerId, conversationId, unreadCount, reason, DateTimeOffset.UtcNow));
    }
}
