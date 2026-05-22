using Synaptix.Backend.Application.Analytics.Models;

namespace Synaptix.Backend.Application.Analytics.Abstractions
{
    public interface IRollupStore
    {
        Task<QuestionAnsweredDailyRollup> UpsertDailyRollupAsync(
            DateOnly utcDate,
            string mode,
            string category,
            int difficulty,
            bool isCorrect,
            int answerTimeMs,
            DateTime answeredAtUtc,
            CancellationToken ct);

        Task<QuestionAnsweredPlayerDailyRollup> UpsertPlayerDailyRollupAsync(
            DateOnly utcDate,
            Guid playerId,
            string mode,
            string category,
            int difficulty,
            bool isCorrect,
            int answerTimeMs,
            DateTime answeredAtUtc,
            CancellationToken ct);
    }
}
