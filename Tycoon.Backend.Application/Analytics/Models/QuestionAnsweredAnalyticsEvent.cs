namespace Tycoon.Backend.Application.Analytics.Models
{
    /// <summary>
    /// Append-only analytics event (stored in Mongo).
    /// This mirrors QuestionAnsweredEvent but is analytics-safe and immutable.
    /// </summary>
    public sealed record QuestionAnsweredAnalyticsEvent(
        string Id,              // deterministic
        Guid MatchId,
        Guid PlayerId,
        string Mode,
        string Category,
        int Difficulty,
        bool IsCorrect,
        int AnswerTimeMs,
        DateTime AnsweredAtUtc
    );
}
