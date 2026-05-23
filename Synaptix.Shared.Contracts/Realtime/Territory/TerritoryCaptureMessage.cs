namespace Synaptix.Shared.Contracts.Realtime.Territory
{
    public sealed record TerritoryCaptureMesage(
        Guid SeasonId,
        int TierNumber,
        string Category,
        Guid NewOwnerId,
        int XpMultiplierBps
    );
}
