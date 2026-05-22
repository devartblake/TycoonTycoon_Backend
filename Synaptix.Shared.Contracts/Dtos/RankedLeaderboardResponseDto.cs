public sealed record RankedLeaderboardResponseDto(
    Guid SeasonId,
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<RankedLeaderboardEntryDto> Items
);
