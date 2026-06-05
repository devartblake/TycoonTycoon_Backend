namespace Synaptix.Backend.Application.Leaderboards
{
    public interface ILeaderboardNotifier
    {
        /// <summary>
        /// Called after a full leaderboard recalculation to push a fresh snapshot
        /// to all subscribed clients on all tier groups.
        /// </summary>
        Task NotifyRecalculatedAsync(CancellationToken ct);

        /// <summary>
        /// Called when a single player's rank changes (e.g. score update).
        /// Pushes the change plus a fresh snapshot for their tier.
        /// </summary>
        Task NotifyRankChangedAsync(
            Guid playerId,
            int tierId,
            int oldRank,
            int newRank,
            int newScore,
            CancellationToken ct);
    }

    public sealed class NullLeaderboardNotifier : ILeaderboardNotifier
    {
        public Task NotifyRecalculatedAsync(CancellationToken ct) => Task.CompletedTask;

        public Task NotifyRankChangedAsync(
            Guid playerId,
            int tierId,
            int oldRank,
            int newRank,
            int newScore,
            CancellationToken ct) => Task.CompletedTask;
    }
}
