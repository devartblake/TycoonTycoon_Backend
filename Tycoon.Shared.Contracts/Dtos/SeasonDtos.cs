namespace Tycoon.Shared.Contracts.Dtos
{
    public enum SeasonStatus
    {
        Scheduled = 1,
        Active = 2,
        Closed = 3
    }

    public sealed record SeasonDto(
        Guid SeasonId,
        int SeasonNumber,
        string Name,
        SeasonStatus Status,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc
    );

    // Admin grid-friendly seasons list
    public sealed record SeasonListResponseDto(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<SeasonDto> Items
    );

    public sealed record CreateSeasonRequest(
        int SeasonNumber,
        string Name,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc
    );

    public sealed record ActivateSeasonRequest(Guid SeasonId);
    public sealed record CloseSeasonRequest(
        Guid SeasonId,
        int CarryoverPercent,    // e.g. 30 means 30% of points carry into next season
        bool CreateNextSeason,
        string? NextSeasonName
    );

    public sealed record PlayerSeasonStateDto(
        Guid PlayerId,
        Guid SeasonId,
        int RankPoints,
        int Wins,
        int Losses,
        int Draws,
        int MatchesPlayed,
        int Tier,
        int TierRank,
        int SeasonRank
    );

    // Admin grid-friendly leaderboard for current season/tier
    public sealed record SeasonLeaderboardItemDto(
        Guid PlayerId,
        int RankPoints,
        int Wins,
        int Losses,
        int Draws,
        int Tier,
        int TierRank,
        int SeasonRank
    );

    public sealed record SeasonLeaderboardResponseDto(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<SeasonLeaderboardItemDto> Items
    );

    // Idempotent season-points apply
    public sealed record ApplySeasonPointsRequest(
        Guid EventId,
        Guid SeasonId,
        Guid PlayerId,
        string Kind,          // "match-result", "admin-adjust", etc.
        int Delta,
        string? Note
    );

    public sealed record ApplySeasonPointsResultDto(
        Guid EventId,
        Guid SeasonId,
        Guid PlayerId,
        string Status,        // "Applied" | "Duplicate"
        int NewRankPoints
    );
}
