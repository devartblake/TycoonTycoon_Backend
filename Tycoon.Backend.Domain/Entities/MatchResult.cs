using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Domain.Entities
{
    /// <summary>
    /// Multiplayer (or multi-participant) match completion snapshot.
    /// Kept separate from Match aggregate to avoid inflating the aggregate and to preserve current lifecycle/events.
    /// </summary>
    public sealed class MatchResult
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid MatchId { get; private set; }          // FK to Match.Id (aggregate Id)
        public Guid SubmitEventId { get; private set; }    // idempotency key (unique)
        public string Mode { get; private set; } = string.Empty;
        public string Category { get; private set; } = string.Empty;
        public int QuestionCount { get; private set; }
        public DateTimeOffset EndedAtUtc { get; private set; }
        public MatchStatus Status { get; private set; }

        public List<MatchParticipantResult> Participants { get; private set; } = new();

        private MatchResult() { } // EF

        public MatchResult(
            Guid matchId,
            Guid submitEventId,
            string mode,
            string category,
            int questionCount,
            DateTimeOffset endedAtUtc,
            MatchStatus status
        )
        {
            MatchId = matchId;
            SubmitEventId = submitEventId;
            Mode = (mode ?? "").Trim();
            Category = (category ?? "").Trim();
            QuestionCount = questionCount;
            EndedAtUtc = endedAtUtc;
            Status = status;
        }
    }

    public sealed class MatchParticipantResult
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid MatchResultId { get; private set; }    // FK to MatchResult.Id
        public Guid PlayerId { get; private set; }

        public int Score { get; private set; }
        public int Correct { get; private set; }
        public int Wrong { get; private set; }
        public double AvgAnswerTimeMs { get; private set; }

        private MatchParticipantResult() { } // EF

        public MatchParticipantResult(Guid matchResultId, Guid playerId, int score, int correct, int wrong, double avgAnswerTimeMs)
        {
            MatchResultId = matchResultId;
            PlayerId = playerId;
            Score = score;
            Correct = correct;
            Wrong = wrong;
            AvgAnswerTimeMs = avgAnswerTimeMs;
        }
    }
}
