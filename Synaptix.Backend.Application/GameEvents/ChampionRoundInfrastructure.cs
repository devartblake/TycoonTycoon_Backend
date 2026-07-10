using Hangfire;
using Mediator;

namespace Synaptix.Backend.Application.GameEvents;

/// <summary>
/// Schedules the delayed resolution of a live round. Abstracted so the
/// orchestrator stays free of Hangfire and is unit-testable.
/// </summary>
public interface IChampionRoundScheduler
{
    void ScheduleResolve(Guid gameEventId, int roundNumber, DateTimeOffset dueUtc);
}

/// <summary>Closes a finished match (prize/jackpot distribution). Abstracted for testing.</summary>
public interface IChampionMatchCloser
{
    Task CloseAsync(Guid gameEventId, CancellationToken ct);
}

/// <summary>Hangfire-backed round scheduler: enqueues a delayed resolve job.</summary>
public sealed class HangfireChampionRoundScheduler : IChampionRoundScheduler
{
    public void ScheduleResolve(Guid gameEventId, int roundNumber, DateTimeOffset dueUtc)
    {
        var delay = dueUtc - DateTimeOffset.UtcNow;
        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
        BackgroundJob.Schedule<ChampionRoundResolveJob>(
            j => j.RunAsync(gameEventId, roundNumber, CancellationToken.None), delay);
    }
}

/// <summary>Hangfire job wrapper that drives a round's resolution at its deadline.</summary>
public sealed class ChampionRoundResolveJob(ChampionMatchOrchestrator orchestrator)
{
    public Task RunAsync(Guid gameEventId, int roundNumber, CancellationToken ct)
        => orchestrator.ResolveRoundAsync(gameEventId, roundNumber, ct);
}

/// <summary>Closes the match via the existing prize-distribution handler.</summary>
public sealed class MediatorChampionMatchCloser(IMediator mediator) : IChampionMatchCloser
{
    public Task CloseAsync(Guid gameEventId, CancellationToken ct)
        => mediator.Send(new CloseGameEventAndDistributePrizes(gameEventId), ct).AsTask();
}
