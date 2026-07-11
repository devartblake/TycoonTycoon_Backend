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
        int EffectiveJackpot = 0,
        string? SponsorName = null
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

    // ── Premium spectator (Champion vs Tier) ──────────────────────────────

    /// <summary>One fallen player in the premium "elimination cam" feed.</summary>
    public sealed record ChampionEliminationDto(
        Guid PlayerId,
        string Handle,
        DateTimeOffset EliminatedAtUtc,
        bool WasChampion,
        int? FinalRank
    );

    /// <summary>
    /// Spectator view of a live match. Everyone gets the basic live counts
    /// (rounds arrive over SignalR); premium pass holders also get the
    /// elimination cam feed. IsPremium tells the client whether to render the
    /// enhanced view or the upsell.
    /// </summary>
    public sealed record ChampionSpectatorViewDto(
        Guid GameEventId,
        bool IsLive,
        bool IsPremium,
        int AliveCount,
        int JackpotPool,
        IReadOnlyList<ChampionEliminationDto> EliminationFeed
    );

    /// <summary>Admin grant of the premium spectator pass (comp/support).</summary>
    public sealed record GrantSpectatorPassRequest(Guid PlayerId, int? Days);

    // ── Sponsor-backed jackpot multiplier ─────────────────────────────────

    /// <summary>
    /// Attribute a jackpot boost to a sponsor. A blank/null SponsorName clears
    /// the attribution (house-funded); Multiplier is clamped to 1.0–10.0.
    /// </summary>
    public sealed record SetEventSponsorRequest(string? SponsorName, decimal Multiplier);

    /// <summary>Preview of an event's sponsor state after a set.</summary>
    public sealed record EventSponsorDto(
        Guid GameEventId,
        string? SponsorName,
        decimal Multiplier,
        int JackpotPool,
        int EffectiveJackpot,
        int BoostAmount
    );

    /// <summary>One closed event's sponsor-funded boost, for reconciliation reports.</summary>
    public sealed record SponsorAttributionDto(
        Guid GameEventId,
        string SponsorName,
        int BaseJackpot,
        decimal Multiplier,
        int EffectiveJackpot,
        int BoostAmount,
        Guid? BeneficiaryPlayerId,
        Guid? SeasonId,
        DateTimeOffset RecordedAtUtc
    );

    /// <summary>Per-sponsor totals across a set of attributions.</summary>
    public sealed record SponsorAttributionSummaryDto(
        string SponsorName,
        int EventsSponsored,
        int TotalBoostFunded
    );
}
