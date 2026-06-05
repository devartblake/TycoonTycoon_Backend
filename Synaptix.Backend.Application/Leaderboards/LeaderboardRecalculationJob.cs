namespace Synaptix.Backend.Application.Leaderboards
{
    /// <summary>
    /// Hangfire-friendly wrapper (parameterless method) for scheduled recalculation.
    /// After recalculation, pushes SignalR snapshots to all subscribed clients
    /// via <see cref="ILeaderboardNotifier"/>.
    /// </summary>
    public sealed class LeaderboardRecalculationJob
    {
        private readonly LeaderboardRecalculator _recalculator;
        private readonly ILeaderboardNotifier _notifier;

        public LeaderboardRecalculationJob(LeaderboardRecalculator recalculator, ILeaderboardNotifier notifier)
        {
            _recalculator = recalculator;
            _notifier     = notifier;
        }

        public async Task Run()
        {
            await _recalculator.RecalculateAsync(CancellationToken.None);
            await _notifier.NotifyRecalculatedAsync(CancellationToken.None);
        }
    }
}
