using Mediator;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Realtime.Clients;
using Synaptix.Backend.Application.Leaderboards;
using Synaptix.Shared.Contracts.Realtime.Leaderboard;

namespace Synaptix.Backend.Api.Realtime
{
    /// <summary>
    /// Pushes leaderboard updates to all subscribed hub clients.
    /// Called by <see cref="LeaderboardRecalculationJob"/> after each batch recalc,
    /// and optionally per-player after a score update.
    ///
    /// Registered as Singleton (matching all other SignalR notifiers).
    /// Uses <see cref="IServiceScopeFactory"/> to create a short-lived scope for each
    /// mediator call — the correct pattern for singletons that need scoped services.
    /// </summary>
    public sealed class SignalRLeaderboardNotifier(
        IHubContext<LeaderboardHub, ILeaderboardClient> hub,
        IServiceScopeFactory scopeFactory) : ILeaderboardNotifier
    {
        private const int SnapshotPageSize = 50;

        public async Task NotifyRecalculatedAsync(CancellationToken ct)
        {
            for (var tierId = 1; tierId <= 6; tierId++)
            {
                var snapshot = await BuildSnapshotAsync(tierId, ct);
                await hub.Clients.Group($"leaderboard:tier:{tierId}").LeaderboardSnapshot(snapshot);
            }

            var globalSnapshot = await BuildSnapshotAsync(tierId: 1, ct);
            await hub.Clients.Group("leaderboard:global").LeaderboardSnapshot(globalSnapshot);
        }

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
            // Create a scope so the scoped IMediator (and its DbContext) is correctly managed.
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(
                new GetTierLeaderboard(tierId, Page: 1, PageSize: SnapshotPageSize), ct);

            var entries = result.Entries.Select(e => new LeaderboardSnapshotEntry(
                e.PlayerId, e.Username, e.CountryCode, e.Score, e.TierRank, e.GlobalRank)).ToList();

            return new LeaderboardSnapshotMessage(tierId, entries, DateTimeOffset.UtcNow);
        }
    }
}
