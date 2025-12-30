using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Events
{
    /// <summary>
    /// Raised when a match ends (win/loss/draw).
    /// Used for mission progression, leaderboard scoring, streaks, etc.
    /// </summary>
    public sealed record MatchCompletedEvent(
        Guid MatchId,
        Guid PlayerId,
        string Mode,
        bool IsWin,
        int ScoreDelta,
        int XpEarned,
        int CorrectAnswers,
        int TotalQuestions,
        int DurationSeconds,
        DateTime CompletedAtUtc
    ) : DomainEvent;
}
