using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Events
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
