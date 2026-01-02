using Microsoft.AspNetCore.SignalR;

namespace Tycoon.Backend.Api.Realtime
{
    public sealed class MatchHub(IConnectionRegistry registry) : Hub
    {
        public async Task JoinMatch(Guid matchId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, $"match:{matchId}");

        public async Task SubmitAnswer(Guid matchId, string answerId)
            => await Clients.Group($"match:{matchId}")
                   .SendAsync("answer_submitted", new { matchId, user = Context.UserIdentifier, answerId });

        public override async Task OnConnectedAsync()
        {
            // Lightweight approach: client passes ?playerId=<guid>
            var playerIdStr = Context.GetHttpContext()?.Request.Query["playerId"].ToString();
            if (Guid.TryParse(playerIdStr, out var playerId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"player:{playerId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var playerIdStr = Context.GetHttpContext()?.Request.Query["playerId"].ToString();

            if (Guid.TryParse(playerIdStr, out var playerId))
            {
                registry.Remove(playerId, Context.ConnectionId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"player:{playerId}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
