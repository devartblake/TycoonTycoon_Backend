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

    /// <summary>The champion called out a challenger for a head-to-head duel.</summary>
    public sealed record ChampionDuelStartedMessage(
        Guid GameEventId,
        Guid DuelId,
        Guid ChampionPlayerId,
        Guid ChallengerPlayerId,
        Guid QuestionId,
        string Prompt,
        IReadOnlyList<ChampionRoundOptionDto> Options,
        DateTimeOffset DeadlineUtc
    );

    /// <summary>A duel resolved: winner stays, loser is out.</summary>
    public sealed record ChampionDuelResolvedMessage(
        Guid GameEventId,
        Guid DuelId,
        Guid WinnerPlayerId,
        Guid LoserPlayerId,
        string CorrectOptionId,
        bool ChampionAlive,
        int SurvivorsRemaining,
        int JackpotPool
    );

    /// <summary>
    /// Point-in-time snapshot for a client joining mid-match (replay-on-join):
    /// the currently open round and/or duel, so the UI can render the live
    /// state immediately instead of waiting for the next broadcast.
    /// </summary>
    public sealed record ChampionLiveSnapshotDto(
        Guid GameEventId,
        int AliveCount,
        int JackpotPool,
        bool IsLive,
        ChampionRoundStartedMessage? CurrentRound,
        ChampionDuelStartedMessage? CurrentDuel
    );
}
