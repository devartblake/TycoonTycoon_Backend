using Microsoft.AspNetCore.SignalR;
using Synaptix.Backend.Api.Realtime.Clients;
using Synaptix.Backend.Application.Matchmaking;
using Synaptix.Shared.Contracts.Realtime.Matchmaking;

namespace Synaptix.Backend.Api.Realtime
{
    /// <summary>
    /// Matchmaking hub — handles queue entry and receives match-found notifications.
    /// Serves browser clients and any client that can't use the gRPC WatchMatchmaking stream.
    ///
    /// Route: /ws/matchmaking
    /// Auth:  JWT via ?access_token=&lt;jwt&gt; or ?playerId=&lt;guid&gt;
    ///
    /// Client flow:
    ///   1. Connect with ?playerId={id}&amp;access_token={jwt}
    ///   2. Call JoinQueue(mode) — server enqueues and sends back matchmaking.queued
    ///   3. Server pushes matchmaking.matched when an opponent is found
    ///   4. Call CancelQueue() to withdraw; server sends matchmaking.cancelled
    /// </summary>
    public sealed class MatchmakingHub(MatchmakingService matchmaking) : Hub<IMatchmakingClient>
    {
        public override async Task OnConnectedAsync()
        {
            var playerIdStr = Context.GetHttpContext()?.Request.Query["playerId"].ToString();
            if (Guid.TryParse(playerIdStr, out var playerId))
                await Groups.AddToGroupAsync(Context.ConnectionId, $"player:{playerId}");

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Enter the matchmaking queue.  If the player is already queued this is idempotent.
        /// On first entry the caller immediately receives a <c>matchmaking.queued</c> event;
        /// when a match is found both players receive <c>matchmaking.matched</c>.
        /// </summary>
        public async Task JoinQueue(string mode, int tier = 1)
        {
            var playerIdStr = Context.GetHttpContext()?.Request.Query["playerId"].ToString();
            if (!Guid.TryParse(playerIdStr, out var playerId))
            {
                await Clients.Caller.Cancelled(new MatchmakingCancelledMessage(
                    Guid.Empty, "INVALID_PLAYER_ID", DateTimeOffset.UtcNow));
                return;
            }

            var result = await matchmaking.EnqueueAsync(playerId, mode, tier, Context.ConnectionAborted);

            if (result.Status == "Forbidden")
            {
                await Clients.Caller.Cancelled(new MatchmakingCancelledMessage(
                    result.TicketId ?? Guid.Empty, "FORBIDDEN", DateTimeOffset.UtcNow));
                return;
            }

            if (result.Status == "Matched")
            {
                await Clients.Caller.Matched(new MatchmakingMatchedMessage(
                    result.TicketId ?? Guid.Empty,
                    result.OpponentId ?? Guid.Empty,
                    mode,
                    tier,
                    DateTimeOffset.UtcNow));
                return;
            }

            // Queued — acknowledge and wait for server-push via IMatchmakingNotifier
            await Clients.Caller.Queued(new MatchmakingQueuedMessage(
                result.TicketId ?? Guid.Empty, mode, tier, DateTimeOffset.UtcNow));
        }

        /// <summary>Withdraw from the matchmaking queue.</summary>
        public async Task CancelQueue()
        {
            var playerIdStr = Context.GetHttpContext()?.Request.Query["playerId"].ToString();
            if (!Guid.TryParse(playerIdStr, out var playerId)) return;

            var status = await matchmaking.GetStatusAsync(playerId, Context.ConnectionAborted);
            await matchmaking.CancelAsync(playerId, Context.ConnectionAborted);

            await Clients.Caller.Cancelled(new MatchmakingCancelledMessage(
                status.TicketId ?? Guid.Empty, "PLAYER_CANCELLED", DateTimeOffset.UtcNow));
        }
    }
}
