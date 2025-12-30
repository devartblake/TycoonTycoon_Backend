using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Events
{
    /// <summary>
    /// Event raised when a season ends.
    /// Final rankings should be computed outside the domain and stored/published as needed.
    /// </summary>
    public sealed record SeasonEndedEvent(
        int SeasonNumber,
        string SeasonName,
        DateTime EndedAtUtc
    ) : DomainEvent;
}
