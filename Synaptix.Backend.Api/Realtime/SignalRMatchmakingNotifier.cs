using Microsoft.AspNetCore.SignalR;
using Synaptix.Backend.Api.Realtime.Clients;
using Synaptix.Backend.Application.Matchmaking;
using Synaptix.Shared.Contracts.Realtime.Matchmaking;

namespace Synaptix.Backend.Api.Realtime
{
    public sealed class SignalRMatchmakingNotifier(
        IHubContext<MatchmakingHub, IMatchmakingClient> matchmakingHub,
        IHubContext<MatchHub> matchHub) : IMatchmakingNotifier
    {
        public async Task NotifyMatchedAsync(
            Guid playerId,
            Guid opponentId,
            string mode,
            int tier,
            string scope,
            Guid ticketId,
            CancellationToken ct)
        {
            var typedMessage = new MatchmakingMatchedMessage(ticketId, opponentId, mode, tier, DateTimeOffset.UtcNow);

            // Push on MatchmakingHub (new — typed, for dedicated matchmaking clients)
            await matchmakingHub.Clients.Group($"player:{playerId}").Matched(typedMessage);

            // Also push on MatchHub (legacy — keeps backward compat with existing Flutter WsClient listeners)
            var legacyPayload = new { TicketId = ticketId, OpponentId = opponentId, Mode = mode, Tier = tier, Scope = scope };
            await matchHub.Clients.Group($"player:{playerId}").SendAsync("matchmaking.matched", legacyPayload, ct);
        }

        public Task NotifyCancelledAsync(Guid playerId, Guid ticketId, string reason, CancellationToken ct)
        {
            var message = new MatchmakingCancelledMessage(ticketId, reason, DateTimeOffset.UtcNow);
            return matchmakingHub.Clients.Group($"player:{playerId}").Cancelled(message);
        }
    }
}
