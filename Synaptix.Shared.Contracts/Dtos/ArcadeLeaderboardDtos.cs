namespace Synaptix.Shared.Contracts.Dtos
{
    public sealed record ArcadeScoreSubmitRequest(
        string GameId,
        string Difficulty,
        int Score,
        int DurationMs
    );

    public sealed record ArcadeLeaderboardEntryDto(
        Guid PlayerId,
        string Username,
        int Score,
        int DurationMs,
        DateTimeOffset AchievedAtUtc,
        int Rank
    );

    public sealed record ArcadeLeaderboardResponseDto(
        string GameId,
        string Difficulty,
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<ArcadeLeaderboardEntryDto> Items,
        int? MyRank,
        int? MyScore
    );
}
