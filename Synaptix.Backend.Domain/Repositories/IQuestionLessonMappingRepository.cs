using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Domain.Repositories;

/// <summary>
/// Repository for accessing question-lesson mappings.
/// Enables looking up lessons related to specific questions.
/// </summary>
public interface IQuestionLessonMappingRepository
{
    /// <summary>
    /// Get all lessons linked to a specific question.
    /// </summary>
    /// <param name="questionId">The question to find lessons for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of lesson IDs linked to the question</returns>
    Task<IEnumerable<Guid>> GetLessonsByQuestionAsync(
        Guid questionId,
        CancellationToken ct = default);

    /// <summary>
    /// Get all questions linked to a specific lesson.
    /// Useful for understanding lesson coverage.
    /// </summary>
    /// <param name="lessonId">The lesson to find questions for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of question IDs linked to the lesson</returns>
    Task<IEnumerable<Guid>> GetQuestionsByLessonAsync(
        Guid lessonId,
        CancellationToken ct = default);

    /// <summary>
    /// Create a new mapping between a question and a lesson.
    /// </summary>
    /// <param name="mapping">The mapping to create</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created mapping</returns>
    Task<QuestionLessonMapping> CreateMappingAsync(
        QuestionLessonMapping mapping,
        CancellationToken ct = default);

    /// <summary>
    /// Delete a mapping between a question and a lesson.
    /// Soft delete - marks as inactive rather than removing from database.
    /// </summary>
    /// <param name="questionId">The question ID</param>
    /// <param name="lessonId">The lesson ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Whether the mapping was found and deleted</returns>
    Task<bool> DeleteMappingAsync(
        Guid questionId,
        Guid lessonId,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a mapping exists between a question and lesson.
    /// </summary>
    /// <param name="questionId">The question ID</param>
    /// <param name="lessonId">The lesson ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if mapping exists and is active</returns>
    Task<bool> MappingExistsAsync(
        Guid questionId,
        Guid lessonId,
        CancellationToken ct = default);

    /// <summary>
    /// Bulk insert multiple mappings.
    /// Used for seeding lesson mappings during initialization.
    /// </summary>
    /// <param name="mappings">Collection of mappings to insert</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of mappings inserted</returns>
    Task<int> BulkInsertMappingsAsync(
        IEnumerable<QuestionLessonMapping> mappings,
        CancellationToken ct = default);
}
