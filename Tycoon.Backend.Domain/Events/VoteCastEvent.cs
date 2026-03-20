using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Events
{
    /// <summary>
    /// Raised when a player casts a vote. Useful for real-time vote tallies and analytics.
    /// </summary>
    public sealed record VoteCastEvent(
        Guid VoteId,
        Guid PlayerId,
        string Option,
        string Topic,
        DateTime CastAtUtc
    ) : DomainEvent;
}
