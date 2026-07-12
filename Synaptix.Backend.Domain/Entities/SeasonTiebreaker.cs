namespace Synaptix.Backend.Domain.Entities;

/// <summary>
/// A scheduled head-to-head that settles a rank-points tie detected at season
/// close (championship or a tier-promotion/reward boundary). While one is
/// pending, the tied players' final rank snapshot rows and season rewards are
/// deferred; resolution (match result, expiry fallback, or admin action)
/// finalizes them.
/// </summary>
public sealed class SeasonTiebreaker
{
    public static class Scopes
    {
        public const string Top1 = "top1";
        public const string TierPromotion = "tier-promotion";
        public const string Custom = "custom";
    }

    public static class Statuses
    {
        public const string Scheduled = "Scheduled";
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string Expired = "Expired";
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid SeasonId { get; private set; }

    /// <summary>See <see cref="Scopes"/>.</summary>
    public string Scope { get; private set; } = Scopes.Custom;

    /// <summary>Tier whose boundary is contested; 0 for the global top spot.</summary>
    public int Tier { get; private set; }

    /// <summary>The contested rank position (1 for top1; the reward rule's MaxTierRank otherwise).</summary>
    public int BoundaryRank { get; private set; }

    /// <summary>The tied rank-points value at detection time (audit/fallback).</summary>
    public int RankPoints { get; private set; }

    public List<Guid> PlayerIds { get; private set; } = new();

    public DateTimeOffset ScheduledAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <summary>See <see cref="Statuses"/>.</summary>
    public string Status { get; private set; } = Statuses.Scheduled;

    public Guid? MatchId { get; private set; }
    public Guid? WinnerPlayerId { get; private set; }
    public string? ResolutionNote { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    private SeasonTiebreaker() { } // EF

    public SeasonTiebreaker(
        Guid seasonId,
        string scope,
        int tier,
        int boundaryRank,
        int rankPoints,
        IEnumerable<Guid> playerIds,
        DateTimeOffset scheduledAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        SeasonId = seasonId;
        Scope = string.IsNullOrWhiteSpace(scope) ? Scopes.Custom : scope.Trim();
        Tier = tier;
        BoundaryRank = boundaryRank;
        RankPoints = rankPoints;
        PlayerIds = playerIds.Distinct().ToList();
        ScheduledAtUtc = scheduledAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Status = Statuses.Scheduled;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public bool IsPending =>
        Status is Statuses.Scheduled or Statuses.InProgress;

    public void AttachMatch(Guid matchId)
    {
        if (!IsPending) return;
        MatchId = matchId;
        Status = Statuses.InProgress;
    }

    public void Resolve(Guid winnerPlayerId, Guid? matchId, string? note)
    {
        WinnerPlayerId = winnerPlayerId;
        MatchId = matchId ?? MatchId;
        ResolutionNote = note;
        Status = Statuses.Completed;
        ResolvedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Expire(Guid fallbackWinnerPlayerId, string? note)
    {
        WinnerPlayerId = fallbackWinnerPlayerId;
        ResolutionNote = note;
        Status = Statuses.Expired;
        ResolvedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel(Guid fallbackWinnerPlayerId, string? note)
    {
        WinnerPlayerId = fallbackWinnerPlayerId;
        ResolutionNote = note;
        Status = Statuses.Cancelled;
        ResolvedAtUtc = DateTimeOffset.UtcNow;
    }
}
