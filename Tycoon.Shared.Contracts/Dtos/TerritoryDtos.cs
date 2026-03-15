namespace Tycoon.Shared.Contracts.Dtos
{
    public enum TerritoryDuelOutcome
    {
        ChallengerWon = 1,
        DefenderWon = 2,
        Draw = 3
    }

    public sealed record TerritoryTileDto(
        string Category,
        Guid? OwnerId,
        int XpMultiplierBps
    );

    public sealed record TerritoryBoardDto(
        Guid SeasonId,
        int TierNumber,
        List<TerritoryTileDto> Tiles
    );

    public sealed record StartTerritoryDuelRequest(
        Guid EventId,
        Guid SeasonId,
        int TierNumber,
        string Category,
        Guid ChallengerId
    );

    public sealed record StartTerritoryDuelResponse(
        Guid MatchId,
        Guid? TileOwnerId
    );
}
