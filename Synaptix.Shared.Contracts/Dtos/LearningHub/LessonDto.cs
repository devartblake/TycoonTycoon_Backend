using System;
using System.Collections.Generic;

namespace Synaptix.Shared.Contracts.Dtos.LearningHub;

/// <summary>
/// Data transfer object for learning lessons.
/// Used to return lesson information to clients.
/// </summary>
public class LessonDto
{
    /// <summary>
    /// Unique identifier for the lesson
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Title of the lesson
    /// Example: "Introduction to Photosynthesis"
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the lesson content
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Topic or category of the lesson
    /// Example: "Biology", "Chemistry", "Physics"
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Difficulty level (1-5)
    /// 1 = Beginner, 5 = Advanced
    /// </summary>
    public int DifficultyLevel { get; set; }

    /// <summary>
    /// Estimated time to complete (in minutes)
    /// </summary>
    public int EstimatedDurationMinutes { get; set; }

    /// <summary>
    /// URL to access the lesson content
    /// </summary>
    public string LessonUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL to a thumbnail image for the lesson
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Category tags for filtering and discovery
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Whether this lesson requires premium access
    /// </summary>
    public bool RequiresPremium { get; set; }

    /// <summary>
    /// Rating out of 5 stars based on user feedback
    /// </summary>
    public decimal? AverageRating { get; set; }

    /// <summary>
    /// Number of users who have completed this lesson
    /// </summary>
    public int CompletionCount { get; set; }
}
