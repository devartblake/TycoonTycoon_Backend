using Synaptix.Backend.Domain.Primitives;

namespace Synaptix.Backend.Domain.Events
{
    /// <summary>
    /// Raised when a match starts. Useful for "play X matches" missions and analytics.
    /// </summary>
    public sealed record MatchStartedEvent(
        Guid MatchId,
        Guid PlayerId,
        string Mode,
        DateTime StartedAtUtc
    ) : DomainEvent;
}
