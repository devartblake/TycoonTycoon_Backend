public sealed record RankedLeaderboardEntryDto(
    Guid PlayerId,
    int SeasonRank,
    int Tier,
    int TierRank,
    int RankPoints,
    int Wins,
    int Losses,
    int Draws,
    int MatchesPlayed
);
