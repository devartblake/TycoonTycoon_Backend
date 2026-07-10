namespace Synaptix.Shared.Contracts.Realtime.GameEvents
{
    /// <summary>One option shown to players during a live round (no answer key).</summary>
    public sealed record ChampionRoundOptionDto(string OptionId, string Text);

    /// <summary>A live round has opened: here's the question and the answer deadline.</summary>
    public sealed record ChampionRoundStartedMessage(
        Guid GameEventId,
        int RoundNumber,
        Guid QuestionId,
        string Prompt,
        IReadOnlyList<ChampionRoundOptionDto> Options,
        DateTimeOffset DeadlineUtc,
        int AliveCount,
        int JackpotPool
    );

    /// <summary>A round resolved: the correct answer, who was eliminated, and who survives.</summary>
    public sealed record ChampionRoundResolvedMessage(
        Guid GameEventId,
        int RoundNumber,
        string CorrectOptionId,
        IReadOnlyList<Guid> EliminatedPlayerIds,
        int SurvivorsRemaining,
        bool ChampionAlive,
        int JackpotPool
    );

    /// <summary>The Champion vs Tier match is over.</summary>
    public sealed record ChampionMatchEndedMessage(
        Guid GameEventId,
        Guid? WinnerPlayerId,
        bool ChampionDefended,
        int JackpotAwarded,
        int RoundsPlayed
    );
}
