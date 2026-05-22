using Synaptix.Backend.Domain.Primitives;

namespace Synaptix.Backend.Domain.Events
{
    /// <summary>
    /// Raised when a player claims a completed mission reward.
    /// </summary>
    public sealed record MissionClaimedEvent(
        Guid PlayerId,
        Guid MissionId,
        int RewardXp
    ) : DomainEvent;
}
