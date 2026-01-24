using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Models;

namespace Tycoon.Backend.Application.Analytics.Writers;

/// <summary>
/// Postgres-backed analytics writer.
/// Persists raw QuestionAnsweredAnalyticsEvent records with an upsert-like behavior.
/// </summary>
public sealed class PostgresAnalyticsEventWriter : IAnalyticsEventWriter
{
    private readonly IAppDb _db;

    public PostgresAnalyticsEventWriter(IAppDb db)
    {
        _db = db;
    }

    public async Task UpsertQuestionAnsweredEventAsync(
            QuestionAnsweredAnalyticsEvent evt,
            CancellationToken ct = default)
    {
        var existing = await _db.QuestionAnsweredAnalyticsEvents
            .FirstOrDefaultAsync(x => x.Id == evt.Id, ct); // Changed to FirstOrDefaultAsync for consistency

        if (existing is null)
        {
            _db.QuestionAnsweredAnalyticsEvents.Add(evt);
        }
        else
        {
            existing.UpdateFrom(evt);
        }

        // CRITICAL: Added SaveChangesAsync so the operation actually persists
        await _db.SaveChangesAsync(ct);
    }

    public async Task WriteQuestionAnsweredAsync(
        Guid playerId,
        string questionId,
        string mode,
        string category,
        int difficulty,
        bool isCorrect,
        int pointsAwarded,
        int answerTimeMs,
        DateTime answeredAtUtc,
        DateTime nowUtc,
        CancellationToken ct)
    {
        // Unique identity (matches [Index] on the model):
        // (PlayerId, QuestionId, AnsweredAtUtc)
        var existing = await _db.QuestionAnsweredAnalyticsEvents
            .SingleOrDefaultAsync(x =>
                x.PlayerId == playerId &&
                x.QuestionId == questionId &&
                x.AnsweredAtUtc == answeredAtUtc,
                ct);

        var incoming = new QuestionAnsweredAnalyticsEvent
        {
            PlayerId = playerId,
            QuestionId = questionId,
            Mode = mode,
            Category = category,
            Difficulty = difficulty,
            IsCorrect = isCorrect,
            PointsAwarded = pointsAwarded,
            AnswerTimeMs = answerTimeMs,
            AnsweredAtUtc = answeredAtUtc,
            UpdatedAtUtc = nowUtc
        };

        if (existing is null)
        {
            _db.QuestionAnsweredAnalyticsEvents.Add(incoming);
        }
        else
        {
            existing.UpdateFrom(incoming);
        }

        await _db.SaveChangesAsync(ct);
    }
}
