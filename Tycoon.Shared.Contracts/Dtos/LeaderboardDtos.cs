namespace Tycoon.Shared.Contracts.Dtos
{
    public record MyTierDto(
        Guid PlayerId,
        int TierId,
        int TierRank,
        int GlobalRank,
        int Score,
        double XpProgress
     );

    public record TierLeaderboardEntryDto(
        Guid PlayerId,
        string Username,
        string CountryCode,
        int Level,
        int Score,
        int GlobalRank,
        int TierRank,
        double XpProgress
    );

    public record TierLeaderboardDto(
        int TierId,
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<TierLeaderboardEntryDto> Entries
    );

    public record LeaderboardRecalcResultDto(
        int PlayersProcessed,
        int TiersUsed,
        int LeaderboardEntriesUpserted,
        DateTimeOffset RecalculatedAtUtc
    );
}
