using System.Text.Json;
using Tycoon.Backend.Domain.Primitives;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class StudySession : Entity
    {
        public Guid PlayerId { get; private set; }
        public string StudySetId { get; private set; } = string.Empty;
        public string Mode { get; private set; } = StudySessionModes.SelfTest;
        public string Title { get; private set; } = string.Empty;
        public string Kind { get; private set; } = string.Empty;
        public string Category { get; private set; } = string.Empty;
        public int QuestionCount { get; private set; }
        public string QuestionIdsJson { get; private set; } = "[]";
        public string AnswerKeyJson { get; private set; } = "{}";
        public string AnsweredResultsJson { get; private set; } = "{}";
        public string InteractionStatesJson { get; private set; } = "{}";
        public int AnsweredCount { get; private set; }
        public int CorrectCount { get; private set; }
        public int CurrentQuestionIndex { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? CompletedAtUtc { get; private set; }

        private StudySession()
        {
        }

        public StudySession(
            Guid playerId,
            string studySetId,
            string mode,
            string title,
            string kind,
            string category,
            IReadOnlyList<Guid> questionIds,
            IReadOnlyDictionary<Guid, string> answerKey)
        {
            PlayerId = playerId;
            StudySetId = studySetId.Trim();
            Mode = NormalizeMode(mode);
            Title = title.Trim();
            Kind = kind.Trim();
            Category = category.Trim();
            QuestionCount = questionIds.Count;
            QuestionIdsJson = JsonSerializer.Serialize(questionIds);
            AnswerKeyJson = JsonSerializer.Serialize(
                answerKey.ToDictionary(x => x.Key.ToString("D"), x => x.Value));
        }

        public void ApplyProgress(
            IReadOnlyDictionary<Guid, bool> answeredResults,
            int currentQuestionIndex,
            bool isCompleted)
        {
            AnsweredResultsJson = JsonSerializer.Serialize(
                answeredResults.ToDictionary(x => x.Key.ToString("D"), x => x.Value));
            AnsweredCount = answeredResults.Count;
            CorrectCount = answeredResults.Count(x => x.Value);
            CurrentQuestionIndex = Math.Clamp(
                currentQuestionIndex,
                0,
                Math.Max(0, QuestionCount - 1));
            UpdatedAtUtc = DateTimeOffset.UtcNow;

            if (isCompleted || AnsweredCount >= QuestionCount)
            {
                CompletedAtUtc ??= DateTimeOffset.UtcNow;
            }
        }

        public void ReplaceInteractionStates(string interactionStatesJson)
        {
            InteractionStatesJson = string.IsNullOrWhiteSpace(interactionStatesJson) ? "{}" : interactionStatesJson;
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        private static string NormalizeMode(string? mode)
        {
            if (string.Equals(mode, StudySessionModes.Flashcard, StringComparison.OrdinalIgnoreCase))
                return StudySessionModes.Flashcard;

            return StudySessionModes.SelfTest;
        }
    }
}
