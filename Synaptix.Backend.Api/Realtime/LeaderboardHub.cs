using Mediator;
using Microsoft.AspNetCore.SignalR;
using Synaptix.Backend.Api.Realtime.Clients;
using Synaptix.Backend.Application.Leaderboards;
using Synaptix.Shared.Contracts.Realtime.Leaderboard;

namespace Synaptix.Backend.Api.Realtime
{
    /// <summary>
    /// Live leaderboard hub — clients subscribe to a tier's leaderboard group
    /// and receive rank-change events pushed by the server after each recalculation.
    ///
    /// Route: /ws/leaderboard
    /// Auth:  JWT via ?access_token=&lt;jwt&gt; or ?playerId=&lt;guid&gt;
    ///
    /// Client flow:
    ///   1. Connect
    ///   2. Call SubscribeTier(tierId) or SubscribeGlobal()
    ///      → joins the appropriate group
    ///      → immediately receives a LeaderboardSnapshot for that tier
    ///   3. Server pushes LeaderboardSnapshot after each recalculation run
    /// </summary>
    public sealed class LeaderboardHub(IMediator mediator) : Hub<ILeaderboardClient>
    {
        /// <summary>Subscribe to a specific tier's leaderboard feed.</summary>
        public async Task SubscribeTier(int tierId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"leaderboard:tier:{tierId}");
            await SendSnapshotAsync(tierId);
        }

        /// <summary>Subscribe to the global leaderboard feed (tier 1 = top tier).</summary>
        public async Task SubscribeGlobal()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "leaderboard:global");
            await SendSnapshotAsync(tierId: 1);
        }

        public Task UnsubscribeTier(int tierId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, $"leaderboard:tier:{tierId}");

        public Task UnsubscribeGlobal() =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, "leaderboard:global");

        private async Task SendSnapshotAsync(int tierId)
        {
            const int snapshotPageSize = 50;
            var result = await mediator.Send(
                new GetTierLeaderboard(tierId, Page: 1, PageSize: snapshotPageSize),
                Context.ConnectionAborted);

            var entries = result.Entries.Select(e => new LeaderboardSnapshotEntry(
                e.PlayerId,
                e.Username,
                e.CountryCode,
                e.Score,
                e.TierRank,
                e.GlobalRank)).ToList();

            await Clients.Caller.LeaderboardSnapshot(
                new LeaderboardSnapshotMessage(tierId, entries, DateTimeOffset.UtcNow));
        }
    }
}
