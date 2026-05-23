namespace Synaptix.Shared.Contracts.Dtos
{
    public sealed record EventLeaderboardEntryDto(
        Guid PlayerId,
        int FinalRank,
        int AwardedXp,
        int AwardedCoins,
        DateTimeOffset? EliminatedAt
    );

    public sealed record PlayerEventHistoryDto(
        Guid GameEventId,
        string Kind,
        int? FinalRank,
        int AwardedXp,
        int AwardedCoins,
        DateTimeOffset EnteredAt
    );

    public sealed record EventSeasonLeaderboardEntryDto(
        Guid PlayerId,
        int EventsWon,
        int EventsTop20,
        int EventsEntered,
        int GuardianDefencesWon,
        int GuardianDaysTotal,
        int CurrentTilesOwned,
        int PeakXpMultiplierBps
    );

    public sealed record TerritoryDominanceDto(
        Guid PlayerId,
        int TilesOwned,
        int TotalXpMultiplierBps
    );
}
