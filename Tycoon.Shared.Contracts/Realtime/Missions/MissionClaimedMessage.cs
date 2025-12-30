namespace Tycoon.Shared.Contracts.Realtime.Missions
{
    public sealed record MissionClaimedMessage(
        Guid PlayerId,
        Guid MissionId,
        string MissionType,
        string MissionKey,
        int RewardXp,
        int RewardCoins,
        int RewardDiamonds,
        DateTime ClaimedAtUtc
    );
}
