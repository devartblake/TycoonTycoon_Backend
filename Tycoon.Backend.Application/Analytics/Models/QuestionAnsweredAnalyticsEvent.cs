using Microsoft.EntityFrameworkCore;
using System;

namespace Tycoon.Backend.Application.Analytics.Models;

[Index(nameof(PlayerId), nameof(QuestionId), nameof(AnsweredAtUtc), IsUnique = true)]
public sealed class QuestionAnsweredAnalyticsEvent
{
    public QuestionAnsweredAnalyticsEvent() { } // Required for EF Core

    public QuestionAnsweredAnalyticsEvent(
        string id,
        Guid matchId,
        Guid playerId,
        string mode,
        string category,
        int difficulty,
        bool isCorrect,
        int answerTimeMs,
        DateTime answeredAtUtc)
    {
        Id = id;
        MatchId = matchId;
        PlayerId = playerId;
        Mode = mode;
        Category = category;
        Difficulty = difficulty;
        IsCorrect = isCorrect;
        AnswerTimeMs = answerTimeMs;
        AnsweredAtUtc = answeredAtUtc;
    }

    public string Id { get; set; } = string.Empty;

    public Guid MatchId { get; set; }
    public Guid PlayerId { get; set; }
    public string QuestionId { get; set; } = string.Empty;

    public string Mode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    // Your IAnalyticsEventWriter uses string difficulty; keep it as string here.
    public int Difficulty { get; set; }

    public bool IsCorrect { get; set; }
    public int PointsAwarded { get; set; }
    public int AnswerTimeMs { get; set; }

    public DateTime AnsweredAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // ── Synaptix analytics dimensions (additive — nullable for backward compatibility) ──
    /// <summary>Synaptix presentation mode: kids, teen, or adult.</summary>
    public string? SynaptixMode { get; set; }
    /// <summary>Platform surface the event originated from: hub, arena, labs, pathways, journey, circles, command.</summary>
    public string? Surface { get; set; }
    /// <summary>Audience segment derived from player profile.</summary>
    public string? AudienceSegment { get; set; }
    /// <summary>Navigation entry point that led to this event (e.g., hub, deep-link, notification).</summary>
    public string? EntryPoint { get; set; }
    /// <summary>Brand version identifier for A/B rollout tracking.</summary>
    public string? BrandVersion { get; set; }

    /// <summary>
    /// Copies mutable fields from <paramref name="src"/> onto this instance.
    /// Used by upsert logic in writers.
    /// </summary>
    public void UpdateFrom(QuestionAnsweredAnalyticsEvent src)
    {
        MatchId = src.MatchId;
        PlayerId = src.PlayerId;
        QuestionId = src.QuestionId;
        Mode = src.Mode;
        Category = src.Category;
        Difficulty = src.Difficulty;
        IsCorrect = src.IsCorrect;
        PointsAwarded = src.PointsAwarded;
        AnswerTimeMs = src.AnswerTimeMs;
        AnsweredAtUtc = src.AnsweredAtUtc;
        SynaptixMode = src.SynaptixMode;
        Surface = src.Surface;
        AudienceSegment = src.AudienceSegment;
        EntryPoint = src.EntryPoint;
        BrandVersion = src.BrandVersion;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
