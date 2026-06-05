using Microsoft.Extensions.Logging;
using Synaptix.Backend.Application.Analytics.Abstractions;
using Synaptix.Backend.Application.Analytics.Models;

namespace Synaptix.Backend.Application.Analytics;

public sealed class QuestionAnsweredAnalyticsPersistence
{
    private readonly IAnalyticsEventWriter _eventWriter;
    private readonly IRollupStore _rollups;
    private readonly IEnumerable<IRollupIndexer> _indexers;
    private readonly ILogger<QuestionAnsweredAnalyticsPersistence> _logger;

    public QuestionAnsweredAnalyticsPersistence(
        IAnalyticsEventWriter eventWriter,
        IRollupStore rollups,
        IEnumerable<IRollupIndexer> indexers,
        ILogger<QuestionAnsweredAnalyticsPersistence> logger)
    {
        _eventWriter = eventWriter;
        _rollups = rollups;
        _indexers = indexers;
        _logger = logger;
    }

    public async Task<QuestionAnsweredAnalyticsPersistenceResult> PersistAsync(
        QuestionAnsweredAnalyticsEvent evt,
        CancellationToken ct = default)
    {
        var inserted = await _eventWriter.UpsertQuestionAnsweredEventAsync(evt, ct);
        if (!inserted)
            return new QuestionAnsweredAnalyticsPersistenceResult(false, null, null);

        var day = DateOnly.FromDateTime(evt.AnsweredAtUtc);
        var daily = await _rollups.UpsertDailyRollupAsync(
            day,
            evt.Mode,
            evt.Category,
            evt.Difficulty,
            evt.IsCorrect,
            evt.AnswerTimeMs,
            evt.AnsweredAtUtc,
            ct);

        var playerDaily = await _rollups.UpsertPlayerDailyRollupAsync(
            day,
            evt.PlayerId,
            evt.Mode,
            evt.Category,
            evt.Difficulty,
            evt.IsCorrect,
            evt.AnswerTimeMs,
            evt.AnsweredAtUtc,
            ct);

        foreach (var indexer in _indexers)
        {
            try
            {
                await indexer.IndexDailyRollupAsync(daily, ct);
                await indexer.IndexPlayerDailyRollupAsync(playerDaily, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Question answered rollup indexing failed for event {EventId}; raw analytics and Mongo/PostgreSQL rollups remain persisted.",
                    evt.Id);
            }
        }

        return new QuestionAnsweredAnalyticsPersistenceResult(true, daily, playerDaily);
    }
}

public sealed record QuestionAnsweredAnalyticsPersistenceResult(
    bool Inserted,
    QuestionAnsweredDailyRollup? DailyRollup,
    QuestionAnsweredPlayerDailyRollup? PlayerDailyRollup);
