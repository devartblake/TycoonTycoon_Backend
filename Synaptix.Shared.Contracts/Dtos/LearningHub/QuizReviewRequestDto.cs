using System;

namespace Synaptix.Shared.Contracts.Dtos.LearningHub;

/// <summary>
/// Request to track a "Learn More" click from the quiz review screen.
/// </summary>
public class LearnMoreClickRequest
{
    /// <summary>
    /// The question that was reviewed
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Context where the click occurred
    /// "quiz-review" = from quiz review screen
    /// "search" = from learning hub search
    /// </summary>
    public string Context { get; set; } = "quiz-review";
}

/// <summary>
/// Response after tracking a learn-more click.
/// </summary>
public class LearnMoreClickResponse
{
    /// <summary>
    /// Whether the click was recorded successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the click was recorded
    /// </summary>
    public DateTimeOffset RecordedAt { get; set; }
}

/// <summary>
/// Request to get recommended lessons based on quiz performance.
/// </summary>
public class RecommendedLessonsRequest
{
    /// <summary>
    /// Category to filter lessons by
    /// Optional - if not provided, all categories are included
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Difficulty level to filter by (1-5)
    /// Optional - if not provided, all difficulties are included
    /// </summary>
    public int? Difficulty { get; set; }

    /// <summary>
    /// Maximum number of lessons to return
    /// Default: 10, Max: 50
    /// </summary>
    public int Limit { get; set; } = 10;
}

/// <summary>
/// Response containing recommended lessons.
/// </summary>
public class RecommendedLessonsResponse
{
    /// <summary>
    /// List of recommended lessons
    /// </summary>
    public List<LessonDto> Lessons { get; set; } = new();

    /// <summary>
    /// Total number of recommendations (before pagination)
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Reason for these recommendations
    /// "based-on-quiz-performance" = Lessons for topics you struggled with
    /// "popular-in-your-level" = Popular lessons at your skill level
    /// </summary>
    public string RecommendationReason { get; set; } = string.Empty;
}
