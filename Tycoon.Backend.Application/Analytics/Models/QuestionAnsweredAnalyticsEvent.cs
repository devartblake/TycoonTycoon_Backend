using System;
using Microsoft.EntityFrameworkCore;

namespace Tycoon.Backend.Application.Analytics.Models;

/// <summary>
/// Raw analytics event for "question answered".
/// Stored for audit/debug and as a source for analytics processing.
/// </summary>
[Index(nameof(PlayerId), nameof(QuestionId), nameof(AnsweredAtUtc), IsUnique = true)]
public sealed class QuestionAnsweredAnalyticsEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PlayerId { get; set; }

    /// <summary>
    /// Question identifier (string to support cross-service IDs).
    /// </summary>
    public string QuestionId { get; set; } = string.Empty;

    public string Mode { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Difficulty { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public int PointsAwarded { get; set; }

    public int AnswerTimeMs { get; set; }

    public DateTime AnsweredAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Copies mutable fields from <paramref name="src"/> onto this instance.
    /// This is used by upsert logic in writers.
    /// </summary>
    public void UpdateFrom(QuestionAnsweredAnalyticsEvent src)
    {
        PlayerId = src.PlayerId;
        QuestionId = src.QuestionId;
        Mode = src.Mode;
        Category = src.Category;
        Difficulty = src.Difficulty;
        IsCorrect = src.IsCorrect;
        PointsAwarded = src.PointsAwarded;
        AnswerTimeMs = src.AnswerTimeMs;
        AnsweredAtUtc = src.AnsweredAtUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
