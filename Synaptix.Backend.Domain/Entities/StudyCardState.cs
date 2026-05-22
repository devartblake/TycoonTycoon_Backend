using Synaptix.Backend.Domain.Primitives;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Domain.Entities
{
    public sealed class StudyCardState : Entity
    {
        public Guid PlayerId { get; private set; }
        public Guid QuestionId { get; private set; }
        public int ReviewCount { get; private set; }
        public int SuccessStreak { get; private set; }
        public decimal EaseFactor { get; private set; } = 2.50m;
        public DateTimeOffset? LastReviewedAtUtc { get; private set; }
        public DateTimeOffset? NextReviewAtUtc { get; private set; }
        public string? LastOutcome { get; private set; }
        public string? LastMode { get; private set; }
        public int? LastConfidence { get; private set; }

        private StudyCardState()
        {
        }

        public StudyCardState(Guid playerId, Guid questionId)
        {
            PlayerId = playerId;
            QuestionId = questionId;
        }

        public void ApplySelfTest(bool isCorrect)
        {
            ReviewCount += 1;
            LastReviewedAtUtc = DateTimeOffset.UtcNow;
            LastMode = StudySessionModes.SelfTest;
            LastOutcome = isCorrect ? "Correct" : "Incorrect";
            LastConfidence = null;

            if (isCorrect)
            {
                SuccessStreak += 1;
                EaseFactor = Math.Min(3.00m, EaseFactor + 0.10m);
                var hours = SuccessStreak switch
                {
                    1 => 24,
                    2 => 72,
                    _ => 24 * 7
                };
                NextReviewAtUtc = LastReviewedAtUtc.Value.AddHours(hours);
            }
            else
            {
                SuccessStreak = 0;
                EaseFactor = Math.Max(1.30m, EaseFactor - 0.20m);
                NextReviewAtUtc = LastReviewedAtUtc.Value.AddHours(6);
            }
        }

        public void ApplyFlashcard(string? flashcardAction, int? confidence)
        {
            ReviewCount += 1;
            LastReviewedAtUtc = DateTimeOffset.UtcNow;
            LastMode = StudySessionModes.Flashcard;
            LastConfidence = confidence.HasValue ? Math.Clamp(confidence.Value, 1, 5) : null;
            var normalizedAction = NormalizeAction(flashcardAction);
            LastOutcome = normalizedAction;

            switch (normalizedAction)
            {
                case "Again":
                    SuccessStreak = 0;
                    EaseFactor = Math.Max(1.30m, EaseFactor - 0.20m);
                    NextReviewAtUtc = LastReviewedAtUtc.Value.AddMinutes(10);
                    break;
                case "Hard":
                    SuccessStreak = Math.Max(0, SuccessStreak);
                    EaseFactor = Math.Max(1.30m, EaseFactor - 0.05m);
                    NextReviewAtUtc = LastReviewedAtUtc.Value.AddHours(12);
                    break;
                case "Easy":
                    SuccessStreak += 1;
                    EaseFactor = Math.Min(3.00m, EaseFactor + 0.15m);
                    NextReviewAtUtc = LastReviewedAtUtc.Value.AddDays(7);
                    break;
                default:
                    SuccessStreak += 1;
                    EaseFactor = Math.Min(3.00m, EaseFactor + 0.05m);
                    NextReviewAtUtc = LastReviewedAtUtc.Value.AddDays(2);
                    break;
            }
        }

        private static string NormalizeAction(string? flashcardAction)
        {
            if (string.Equals(flashcardAction, "Again", StringComparison.OrdinalIgnoreCase))
                return "Again";
            if (string.Equals(flashcardAction, "Hard", StringComparison.OrdinalIgnoreCase))
                return "Hard";
            if (string.Equals(flashcardAction, "Easy", StringComparison.OrdinalIgnoreCase))
                return "Easy";
            return "Good";
        }
    }
}
