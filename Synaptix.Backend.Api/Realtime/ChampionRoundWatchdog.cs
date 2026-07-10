using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.GameEvents;

namespace Synaptix.Backend.Api.Realtime
{
    /// <summary>
    /// Redundancy driver for live champion matches. Hangfire schedules each
    /// round/duel resolution at its deadline; this hosted loop is the safety
    /// net that sweeps for anything Hangfire dropped (lost job, restart, clock
    /// skew) and resolves it. Resolution is idempotent, so the two drivers
    /// never double-fire — the watchdog only ever catches what slipped through.
    /// </summary>
    public sealed class ChampionRoundWatchdog(
        IServiceScopeFactory scopeFactory,
        IOptions<ChampionRoundOptions> options,
        ILogger<ChampionRoundWatchdog> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var period = TimeSpan.FromSeconds(Math.Max(1, options.Value.WatchdogPollSeconds));
            using var timer = new PeriodicTimer(period);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var orchestrator = scope.ServiceProvider.GetRequiredService<ChampionMatchOrchestrator>();

                    var rounds = await orchestrator.ResolveOverdueRoundsAsync(stoppingToken);
                    var duels = await orchestrator.ResolveOverdueDuelsAsync(stoppingToken);

                    if (rounds > 0 || duels > 0)
                        logger.LogWarning(
                            "ChampionRoundWatchdog swept {Rounds} overdue round(s) and {Duels} overdue duel(s) that the primary scheduler missed.",
                            rounds, duels);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // A bad tick must never kill the loop.
                    logger.LogError(ex, "ChampionRoundWatchdog sweep failed; will retry next tick.");
                }
            }
        }
    }
}
