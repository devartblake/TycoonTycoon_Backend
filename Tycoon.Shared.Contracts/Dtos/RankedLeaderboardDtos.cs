namespace Tycoon.Shared.Contracts.Dtos;

public sealed record RankedLeaderboardQueryDto(
    Guid? SeasonId,
    string Scope,            // "global" | "tier"
    int? Tier,               // optional tier number if you expose it
    int Page,
    int PageSize,
    string Sort              // "points" | "tierRank"
);

public sealed record RankedLeaderboardItemDto(
    Guid PlayerId,
    // Canonical names (match the domain model)
    int RankPoints,
    int SeasonRank,

    int TierRank,
    int Tier,                      // derived if you store it
    int Wins,
    int Losses,
    int Draws,
    int PlacementMatchesCompleted,
    bool IsPlacement,
    bool EligibleForPromotion,
    bool EligibleForDailyReward,
    DateTimeOffset LastUpdatedUtc
)
{
    // Aliases for backward/UI convenience
    public int SeasonPoints => RankPoints;
    public int GlobalRank => SeasonRank;
};

public sealed record RankedLeaderboardGridResponseDto(
    int Page,
    int PageSize,
    int Total,
    string Scope,
    Guid SeasonId,
    int? Tier,
    IReadOnlyList<RankedLeaderboardItemDto> Items,
    // grid-friendly extras for client:
    IReadOnlyDictionary<string, string> Meta,
    IReadOnlyList<string> Columns
);
