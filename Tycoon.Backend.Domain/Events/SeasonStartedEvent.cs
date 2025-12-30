using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Events
{
    /// <summary>
    /// Event raised when a new season starts.
    /// Typically triggers leaderboard reset + mission rotation in Application/Infrastructure.
    /// </summary>
    public sealed record SeasonStartedEvent(
        int SeasonNumber,
        string SeasonName,
        DateTime StartDateUtc,
        DateTime EndDateUtc
    ) : DomainEvent;
}
