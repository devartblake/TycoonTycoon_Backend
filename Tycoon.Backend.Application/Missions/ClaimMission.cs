using MediatR;

namespace Tycoon.Backend.Application.Missions
{
    public enum ClaimMissionStatus
    {
        Claimed = 0,
        AlreadyClaimed = 1,
        NotCompleted = 2,
        NotFound = 3
    }

    public sealed record MissionListItem(
        Guid MissionId,
        string Type,
        string Key,
        int Goal,
        bool Active,
        int Progress,
        bool Completed,
        bool Claimed
    );

    public sealed record ClaimMission(Guid PlayerId, Guid MissionId, string TypeFilter) : IRequest<ClaimMissionResult>;

    public sealed record ClaimMissionResult(
        ClaimMissionStatus Status,
        Guid PlayerId,
        Guid MissionId,

        // For realtime message
        string MissionType,
        string MissionKey,
        int RewardXp,
        int RewardCoins,
        int RewardDiamonds,
        DateTime ClaimedAtUtcUtc,

        // Claim snapshot
        bool Completed,
        bool Claimed,
        int Progress,
        int Goal,

        // Updated list returned to client
        IReadOnlyList<MissionListItem> UpdatedMissions
    );
}
