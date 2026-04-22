using System.Text.Json;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Study
{
    internal static class StudySessionMapper
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public static IReadOnlyList<Guid> ReadQuestionIds(StudySession session)
        {
            return JsonSerializer.Deserialize<List<Guid>>(session.QuestionIdsJson, JsonOptions) ?? [];
        }

        public static Dictionary<Guid, string> ReadAnswerKey(StudySession session)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(session.AnswerKeyJson, JsonOptions) ?? [];
            return data
                .Where(x => Guid.TryParse(x.Key, out _))
                .ToDictionary(x => Guid.Parse(x.Key), x => x.Value);
        }

        public static Dictionary<Guid, bool> ReadAnsweredResults(StudySession session)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, bool>>(session.AnsweredResultsJson, JsonOptions) ?? [];
            return data
                .Where(x => Guid.TryParse(x.Key, out _))
                .ToDictionary(x => Guid.Parse(x.Key), x => x.Value);
        }

        public static Dictionary<Guid, StoredStudyInteraction> ReadInteractionStates(StudySession session)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, StoredStudyInteraction>>(session.InteractionStatesJson, JsonOptions) ?? [];
            return data
                .Where(x => Guid.TryParse(x.Key, out _))
                .ToDictionary(x => Guid.Parse(x.Key), x => x.Value);
        }

        public static StudySessionDto ToDto(StudySession session)
        {
            var questionIds = ReadQuestionIds(session);
            var answeredResults = ReadAnsweredResults(session);
            var interactions = ReadInteractionStates(session);

            return new StudySessionDto(
                session.Id,
                session.StudySetId,
                session.Mode,
                session.Title,
                session.Kind,
                session.Category,
                session.QuestionCount,
                session.AnsweredCount,
                session.CorrectCount,
                session.CurrentQuestionIndex,
                session.CompletedAtUtc.HasValue,
                questionIds,
                answeredResults.Keys.OrderBy(x => x).ToList(),
                interactions
                    .OrderBy(x => x.Key)
                    .Select(x => new StudySessionInteractionDto(
                        x.Key,
                        x.Value.FlashcardAction,
                        x.Value.Confidence,
                        x.Value.AnswerRevealed,
                        x.Value.LastInteractedAtUtc))
                    .ToList(),
                session.CreatedAtUtc,
                session.UpdatedAtUtc,
                session.CompletedAtUtc);
        }
    }
}
