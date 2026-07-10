namespace Synaptix.Backend.Application.GameEvents;

public sealed record ChampionRoundOptions
{
    /// <summary>Seconds players have to answer each round.</summary>
    public int AnswerWindowSeconds { get; init; } = 12;

    /// <summary>Hard cap on rounds so a match always ends (champion defends if still alive at the cap).</summary>
    public int MaxRounds { get; init; } = 15;

    /// <summary>How many candidate questions to sample from when picking a fresh one per round.</summary>
    public int QuestionSampleSize { get; init; } = 50;

    /// <summary>How often the redundancy watchdog sweeps for overdue rounds/duels.</summary>
    public int WatchdogPollSeconds { get; init; } = 5;

    /// <summary>
    /// How long past a deadline the watchdog waits before resolving, so it
    /// doesn't race the primary Hangfire job. Resolution is idempotent either way.
    /// </summary>
    public int WatchdogGraceSeconds { get; init; } = 3;

    /// <summary>Seconds each head-to-head champion duel stays open.</summary>
    public int DuelWindowSeconds { get; init; } = 12;

    /// <summary>Maximum duels a champion may initiate per match (prevents free culling).</summary>
    public int MaxDuelsPerMatch { get; init; } = 3;
}
