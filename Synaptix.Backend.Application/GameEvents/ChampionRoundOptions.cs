namespace Synaptix.Backend.Application.GameEvents;

public sealed record ChampionRoundOptions
{
    /// <summary>Seconds players have to answer each round.</summary>
    public int AnswerWindowSeconds { get; init; } = 12;

    /// <summary>Hard cap on rounds so a match always ends (champion defends if still alive at the cap).</summary>
    public int MaxRounds { get; init; } = 15;

    /// <summary>How many candidate questions to sample from when picking a fresh one per round.</summary>
    public int QuestionSampleSize { get; init; } = 50;
}
