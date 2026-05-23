using Synaptix.Backend.Domain.Primitives;

namespace Synaptix.Backend.Domain.Events
{
    /// <summary>
    /// Raised when a player completes a mission (goal reached).
    /// Claiming reward is a separate event.
    /// </summary>
    public sealed record MissionCompletedEvent(
        Guid PlayerId,
        Guid MissionId,
        string MissionType,
        string MissionKey,
        int RewardXp
    ) : DomainEvent;
}
