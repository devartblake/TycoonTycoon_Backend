using Microsoft.AspNetCore.SignalR;
using Tycoon.Backend.Api.Realtime.Clients;
using Tycoon.Backend.Application.Realtime;
using Tycoon.Shared.Contracts.Realtime.Notifications;

namespace Tycoon.Backend.Api.Realtime
{
    public sealed class SignalRPlayerNotificationNotifier(IHubContext<NotificationHub, INotificationClient> hub)
        : IPlayerNotificationNotifier
    {
        public Task NotifyInboxUpdatedAsync(Guid playerId, int unreadCount, string reason, CancellationToken ct)
            => hub.Clients.Group($"player:{playerId}")
                .NotificationInboxUpdated(new NotificationInboxUpdatedMessage(playerId, unreadCount, reason, DateTimeOffset.UtcNow));
    }
}
