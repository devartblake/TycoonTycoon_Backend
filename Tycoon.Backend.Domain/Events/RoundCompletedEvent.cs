using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Events
{
    /// <summary>
    /// Summary event for a set of questions (a "round").
    /// Useful for mission updates without processing each question individually.
    /// </summary>
    public sealed record RoundCompletedEvent(
        Guid MatchId,
        Guid PlayerId,
        int RoundIndex,
        int Correct,
        int Total,
        int AvgAnswerTimeMs,
        bool PerfectRound,
        DateTime CompletedAtUtc
    ) : DomainEvent;
}
