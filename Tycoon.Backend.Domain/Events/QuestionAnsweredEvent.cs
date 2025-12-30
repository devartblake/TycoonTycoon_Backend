using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Events
{
    /// <summary>
    /// Raised when a player answers a question.
    /// Keep the payload compact; analytics-heavy data belongs in projections.
    /// </summary>
    public sealed record QuestionAnsweredEvent(
        Guid MatchId,
        Guid PlayerId,
        string Mode,
        string Category,
        int Difficulty,
        bool IsCorrect,
        int AnswerTimeMs,
        DateTime AnsweredAtUtc
    ) : DomainEvent;
}
