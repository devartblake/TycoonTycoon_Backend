using Microsoft.AspNetCore.SignalR;
using Tycoon.Backend.Api.Realtime.Clients;

namespace Tycoon.Backend.Api.Realtime
{
    public class NotificationHub : Hub<INotificationClient>
    {
        /// <summary>
        /// Automatic group join via query string (?playerId=...)
        /// Useful for simple clients and early bootstrapping.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var playerId = Context.GetHttpContext()?.Request.Query["playerId"].ToString();
            if (Guid.TryParse(playerId, out var pid))
                await Groups.AddToGroupAsync(Context.ConnectionId, $"player:{pid}");

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Explicit join method for authenticated or late-bound clients.
        /// Recommended for production clients.
        /// </summary>
        public Task JoinPlayer(Guid playerId) => Groups.AddToGroupAsync(Context.ConnectionId, $"player:{playerId}");        
    }
}
