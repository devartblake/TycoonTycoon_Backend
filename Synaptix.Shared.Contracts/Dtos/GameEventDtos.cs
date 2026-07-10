namespace Synaptix.Shared.Contracts.Dtos
{
    public enum GameEventStatus
    {
        Scheduled = 1,
        Open = 2,
        Live = 3,
        Closed = 4
    }

    public sealed record CreateGameEventRequest(
        string Kind,
        int TierId,
        DateTimeOffset ScheduledAtUtc,
        DateTimeOffset? OpenAtUtc,
        int EntryFeeCoins,
        int ReviveCostGems,
        int MaxParticipants
    );

    public sealed record GameEventSummaryDto(
        Guid Id,
        string Kind,
        int TierId,
        GameEventStatus Status,
        DateTimeOffset ScheduledAtUtc,
        int EntryFeeCoins,
        int MaxParticipants
    );

    public sealed record GameEventStatusDto(
        Guid Id,
        string Kind,
        GameEventStatus Status,
        DateTimeOffset ScheduledAtUtc,
        int ParticipantCount,
        int AliveCount,
        int JackpotPool,
        int TierId = 0,
        int MaxParticipants = 0,
        int EntryFeeCoins = 0,
        Guid? ChampionPlayerId = null,
        decimal JackpotMultiplier = 1.0m,
        int EffectiveJackpot = 0
    );

    public sealed record EnterGameEventRequest(
        Guid EventId,
        Guid GameEventId,
        Guid PlayerId
    );

    public sealed record EnterGameEventResponse(
        Guid EventId,
        string Status
    );

    public sealed record ReviveInGameEventRequest(
        Guid EventId,
        Guid GameEventId,
        Guid PlayerId
    );

    public sealed record ReviveResponse(
        Guid EventId,
        string Status,
        int RevivesUsed
    );

    public sealed record CloseGameEventResponse(
        Guid GameEventId,
        int TotalParticipants,
        int JackpotDistributed
    );

    // ── No-loss predictions (Champion vs Tier) ────────────────────────────

    public sealed record SubmitPredictionRequest(bool ChampionDefends);

    /// <summary>
    /// A spectator's prediction state for a champion_vs_tier event: whether
    /// predictions are still open, the caller's pick, the live tally, the
    /// reward pool, and — once the match is over — their result.
    /// </summary>
    public sealed record ChampionPredictionStateDto(
        Guid GameEventId,
        bool Open,
        bool? MyPrediction,
        int DefendCount,
        int DethroneCount,
        int RewardCoinPool,
        bool Resolved,
        bool? WasCorrect,
        int RewardCoins,
        int RewardXp
    );
}
