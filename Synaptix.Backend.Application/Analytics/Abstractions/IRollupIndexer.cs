using Synaptix.Backend.Application.Analytics.Models;

namespace Synaptix.Backend.Application.Analytics.Abstractions
{
    public interface IRollupIndexer
    {
        Task IndexDailyRollupAsync(QuestionAnsweredDailyRollup rollup, CancellationToken ct);
        Task IndexPlayerDailyRollupAsync(QuestionAnsweredPlayerDailyRollup rollup, CancellationToken ct);
    }
}
