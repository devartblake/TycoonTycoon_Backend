using Microsoft.AspNetCore.SignalR;
using Tycoon.Backend.Application.Matchmaking;

namespace Tycoon.Backend.Api.Realtime
{
    public sealed class SignalRMatchmakingNotifier(IHubContext<MatchHub> hub) : IMatchmakingNotifier
    {
        public Task NotifyMatchedAsync(
            Guid playerId,
            Guid opponentId,
            string mode,
            int tier,
            string scope,
            Guid ticketId,
            CancellationToken ct)
        {
            var payload = new
            {
                TicketId = ticketId,
                OpponentId = opponentId,
                Mode = mode,
                Tier = tier,
                Scope = scope
            };

            // Send to the player’s group
            return hub.Clients.Group($"player:{playerId}")
                .SendAsync("matchmaking.matched", payload, ct);
        }
    }
}
