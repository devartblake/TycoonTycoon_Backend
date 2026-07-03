using System;

namespace Synaptix.Backend.Domain.Entities;

/// <summary>
/// Maps quiz questions to learning resources (lessons).
/// Enables the "Learn More" feature that links incorrect answers to lessons.
/// </summary>
public class QuestionLessonMapping
{
    /// <summary>
    /// Unique identifier for this mapping
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the quiz question
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Reference to the learning lesson
    /// </summary>
    public Guid LessonId { get; set; }

    /// <summary>
    /// Topic or category that connects the question to the lesson
    /// Example: "Mathematics", "History", "Science"
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Difficulty level of the associated lesson (1-5)
    /// Helps recommend appropriate lessons based on question difficulty
    /// </summary>
    public int DifficultyLevel { get; set; }

    /// <summary>
    /// Human-readable description of the relationship
    /// Example: "This question tests knowledge covered in..."
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When this mapping was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this mapping was last updated
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Whether this mapping is active
    /// Can be soft-deleted by setting to false
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties (if using EF Core)
    // public Question Question { get; set; }
    // public Lesson Lesson { get; set; }
}
