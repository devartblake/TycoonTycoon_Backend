using Microsoft.AspNetCore.SignalR;
using Synaptix.Backend.Api.Realtime.Clients;
using Synaptix.Backend.Application.Realtime;
using Synaptix.Shared.Contracts.Realtime.Notifications;

namespace Synaptix.Backend.Api.Realtime
{
    public sealed class SignalRPlayerNotificationNotifier(IHubContext<NotificationHub, INotificationClient> hub)
        : IPlayerNotificationNotifier
    {
        public Task NotifyInboxUpdatedAsync(Guid playerId, int unreadCount, string reason, CancellationToken ct)
            => hub.Clients.Group($"player:{playerId}")
                .NotificationInboxUpdated(new NotificationInboxUpdatedMessage(playerId, unreadCount, reason, DateTimeOffset.UtcNow));
    }
}
