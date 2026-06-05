using Mediator;
using Microsoft.AspNetCore.SignalR;
using Synaptix.Backend.Api.Realtime.Clients;
using Synaptix.Backend.Application.Leaderboards;
using Synaptix.Shared.Contracts.Realtime.Leaderboard;

namespace Synaptix.Backend.Api.Realtime
{
    /// <summary>
    /// Pushes leaderboard updates to all subscribed hub clients.
    /// Called by <see cref="LeaderboardRecalculationJob"/> after each batch recalc,
    /// and optionally per-player after a score update.
    /// </summary>
    public sealed class SignalRLeaderboardNotifier(
        IHubContext<LeaderboardHub, ILeaderboardClient> hub,
        IMediator mediator) : ILeaderboardNotifier
    {
        private const int SnapshotPageSize = 50;

        /// <summary>
        /// Push a fresh tier snapshot to all subscribed clients after a full recalculation.
        /// Iterates tiers 1–6 (the standard Synaptix tier ladder).
        /// </summary>
        public async Task NotifyRecalculatedAsync(CancellationToken ct)
        {
            for (var tierId = 1; tierId <= 6; tierId++)
            {
                var snapshot = await BuildSnapshotAsync(tierId, ct);
                await hub.Clients.Group($"leaderboard:tier:{tierId}").LeaderboardSnapshot(snapshot);
            }

            // Push tier-1 snapshot to the "global" group as well.
            var globalSnapshot = await BuildSnapshotAsync(tierId: 1, ct);
            await hub.Clients.Group("leaderboard:global").LeaderboardSnapshot(globalSnapshot);
        }

        /// <summary>
        /// Push a rank-change event and a fresh snapshot for a single player's tier.
        /// </summary>
        public async Task NotifyRankChangedAsync(
            Guid playerId,
            int tierId,
            int oldRank,
            int newRank,
            int newScore,
            CancellationToken ct)
        {
            var rankChanged = new LeaderboardRankChangedMessage(
                playerId, tierId, oldRank, newRank, newScore, DateTimeOffset.UtcNow);

            await hub.Clients.Group($"leaderboard:tier:{tierId}").RankChanged(rankChanged);
            await hub.Clients.Group("leaderboard:global").RankChanged(rankChanged);

            var snapshot = await BuildSnapshotAsync(tierId, ct);
            await hub.Clients.Group($"leaderboard:tier:{tierId}").LeaderboardSnapshot(snapshot);
        }

        private async Task<LeaderboardSnapshotMessage> BuildSnapshotAsync(int tierId, CancellationToken ct)
        {
            var result = await mediator.Send(
                new GetTierLeaderboard(tierId, Page: 1, PageSize: SnapshotPageSize), ct);

            var entries = result.Entries.Select(e => new LeaderboardSnapshotEntry(
                e.PlayerId, e.Username, e.CountryCode, e.Score, e.TierRank, e.GlobalRank)).ToList();

            return new LeaderboardSnapshotMessage(tierId, entries, DateTimeOffset.UtcNow);
        }
    }
}
