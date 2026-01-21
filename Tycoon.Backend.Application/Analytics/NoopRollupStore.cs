using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Models;

namespace Tycoon.Backend.Application.Analytics;

/// <summary>
/// Null-object rollup store used when rollup persistence is disabled or not configured.
/// Returns null-forgiving defaults because callers typically do not rely on the returned value
/// (they mainly need side-effects/persistence).
/// </summary>
public sealed class NoopRollupStore : IRollupStore
{
    public Task<QuestionAnsweredDailyRollup> UpsertDailyRollupAsync(
        DateOnly utcDate,
        string mode,
        string category,
        int difficulty,
        bool isCorrect,
        int answerTimeMs,
        DateTime answeredAtUtc,
        CancellationToken ct)
    {
        // If your codebase requires a non-null concrete instance here,
        // replace this with a real in-memory rollup instance builder.
        return Task.FromResult(default(QuestionAnsweredDailyRollup)!);
    }

    public Task<QuestionAnsweredPlayerDailyRollup> UpsertPlayerDailyRollupAsync(
        DateOnly utcDate,
        Guid playerId,
        string mode,
        string category,
        int difficulty,
        bool isCorrect,
        int answerTimeMs,
        DateTime answeredAtUtc,
        CancellationToken ct)
    {
        return Task.FromResult(default(QuestionAnsweredPlayerDailyRollup)!);
    }
}
