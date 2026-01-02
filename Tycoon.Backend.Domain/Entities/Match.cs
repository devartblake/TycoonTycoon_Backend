using Tycoon.Backend.Domain.Events;
using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Entities
{
    /// <summary>
    /// Match aggregate (domain). Raises domain events for lifecycle and gameplay activity.
    /// </summary>
    public sealed class Match : AggregateRoot
    {
        public Guid HostPlayerId { get; private set; }
        public string Mode { get; private set; } = "solo";
        public DateTimeOffset StartedAt { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? FinishedAt { get; private set; }
        public byte[]? RowVersion { get; private set; } // EF Core concurrency token

        /// <summary>
        /// Stores per-question outcomes (historical log).
        /// Note: This currently represents "question attempts", not "round summaries".
        /// </summary>
        public List<MatchRound> Rounds { get; private set; } = new();
        public object CreatedAtUtc { get; set; }

        private Match() { } // EF Core

        public Match(Guid hostPlayerId, string mode)
        {
            HostPlayerId = hostPlayerId;
            Mode = string.IsNullOrWhiteSpace(mode) ? "solo" : mode;
            StartedAt = DateTimeOffset.UtcNow;

            // Domain event: match started
            Raise(new MatchStartedEvent(
                MatchId: Id,
                PlayerId: HostPlayerId,
                Mode: Mode,
                StartedAtUtc: StartedAt.UtcDateTime
            ));
        }

        private void EnsureNotFinished()
        {
            if (FinishedAt is not null)
                throw new InvalidOperationException("Match is already finished.");
        }

        /// <summary>
        /// Records a question answer. Use for granular mission progression.
        /// Consider processing these in background if volume increases.
        /// </summary>
        public void RecordAnswer(
            string category,
            int difficulty,
            bool isCorrect,
            int answerTimeMs,
            int points = 0)
        {
            EnsureNotFinished();

            // Persist local state (per-question record)
            var nextIndex = Rounds.Count == 0 ? 0 : Rounds.Max(r => r.Index) + 1;
            Rounds.Add(new MatchRound(
                matchId: Id,
                index: nextIndex,
                correct: isCorrect,
                answerTimeMs: answerTimeMs,
                points: points
            ));

            // Raise domain event (analytics/progression)
            Raise(new QuestionAnsweredEvent(
                MatchId: Id,
                PlayerId: HostPlayerId,
                Mode: Mode,
                Category: category ?? "",
                Difficulty: difficulty,
                IsCorrect: isCorrect,
                AnswerTimeMs: answerTimeMs,
                AnsweredAtUtc: DateTime.UtcNow
            ));
        }

        /// <summary>
        /// Records the completion of a "round" (a group of questions).
        /// Recommended for synchronous mission progression updates.
        ///
        /// NOTE: This method expects a summary payload. You can compute these stats
        /// from Rounds in the application layer if needed.
        /// </summary>
        public void CompleteRound(
            int roundIndex,
            int correct,
            int total,
            int avgAnswerTimeMs,
            bool perfectRound)
        {
            EnsureNotFinished();

            Raise(new RoundCompletedEvent(
                MatchId: Id,
                PlayerId: HostPlayerId,
                RoundIndex: roundIndex,
                Correct: correct,
                Total: total,
                AvgAnswerTimeMs: avgAnswerTimeMs,
                PerfectRound: perfectRound,
                CompletedAtUtc: DateTime.UtcNow
            ));
        }

        /// <summary>
        /// Finishes the match and raises MatchCompletedEvent.
        /// Recommended for synchronous mission progression updates.
        /// </summary>
        public void Finish(
            bool isWin,
            int scoreDelta,
            int xpEarned,
            int correctAnswers,
            int totalQuestions,
            int durationSeconds)
        {
            if (FinishedAt is not null)
                return; // idempotent finish

            FinishedAt = DateTimeOffset.UtcNow;

            Raise(new MatchCompletedEvent(
                MatchId: Id,
                PlayerId: HostPlayerId,
                Mode: Mode,
                IsWin: isWin,
                ScoreDelta: scoreDelta,
                XpEarned: xpEarned,
                CorrectAnswers: correctAnswers,
                TotalQuestions: totalQuestions,
                DurationSeconds: durationSeconds,
                CompletedAtUtc: FinishedAt.Value.UtcDateTime
            ));
        }

        /// <summary>
        /// Backwards-compatible finish method (no payload).
        /// Prefer the overload with results when you wire gameplay.
        /// </summary>
        public void Finish()
        {
            if (FinishedAt is not null)
                return;

            FinishedAt = DateTimeOffset.UtcNow;
        }
    }
}
