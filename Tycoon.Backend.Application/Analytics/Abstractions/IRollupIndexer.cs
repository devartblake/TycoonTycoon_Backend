using Tycoon.Backend.Application.Analytics.Models;

namespace Tycoon.Backend.Application.Analytics.Abstractions
{
    public interface IRollupIndexer
    {
        Task IndexDailyRollupAsync(QuestionAnsweredDailyRollup rollup, CancellationToken ct);
        Task IndexPlayerDailyRollupAsync(QuestionAnsweredPlayerDailyRollup rollup, CancellationToken ct);
    }
}
