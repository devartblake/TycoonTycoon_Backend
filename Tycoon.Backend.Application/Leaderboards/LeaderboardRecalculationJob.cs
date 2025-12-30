namespace Tycoon.Backend.Application.Leaderboards
{
    /// <summary>
    /// Hangfire-friendly wrapper (parameterless method) for scheduled recalculation.
    /// </summary>
    public sealed class LeaderboardRecalculationJob
    {
        private readonly LeaderboardRecalculator _recalculator;

        public LeaderboardRecalculationJob(LeaderboardRecalculator recalculator)
        {
            _recalculator = recalculator;
        }

        public async Task Run()
        {
            await _recalculator.RecalculateAsync(CancellationToken.None);
        }
    }
}
