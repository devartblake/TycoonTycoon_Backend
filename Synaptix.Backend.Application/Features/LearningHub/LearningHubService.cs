using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synaptix.Backend.Domain.Repositories;
using Synaptix.Shared.Contracts.Dtos.LearningHub;

namespace Synaptix.Backend.Application.Features.LearningHub;

/// <summary>
/// Service for managing learning hub features.
/// Handles linking questions to lessons and tracking engagement.
/// </summary>
public interface ILearningHubService
{
    /// <summary>
    /// Get lessons related to a specific question.
    /// </summary>
    Task<IEnumerable<Guid>> GetLessonsForQuestionAsync(
        Guid questionId,
        CancellationToken ct = default);

    /// <summary>
    /// Track when a user clicks "Learn More" from quiz review.
    /// </summary>
    Task<bool> TrackLearnMoreClickAsync(
        Guid playerId,
        Guid questionId,
        string context = "quiz-review",
        CancellationToken ct = default);

    /// <summary>
    /// Get recommended lessons for a player based on their performance.
    /// </summary>
    Task<List<LessonDto>> GetRecommendedLessonsAsync(
        Guid playerId,
        string? category = null,
        int? difficulty = null,
        int limit = 10,
        CancellationToken ct = default);
}

/// <summary>
/// Implementation of ILearningHubService.
/// </summary>
public class LearningHubService : ILearningHubService
{
    private readonly IQuestionLessonMappingRepository _mappingRepository;
    private readonly IAnalyticsEventService _analyticsService;
    private readonly ILogger<LearningHubService> _logger;

    public LearningHubService(
        IQuestionLessonMappingRepository mappingRepository,
        IAnalyticsEventService analyticsService,
        ILogger<LearningHubService> logger)
    {
        _mappingRepository = mappingRepository;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<IEnumerable<Guid>> GetLessonsForQuestionAsync(
        Guid questionId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Fetching lessons for question {QuestionId}",
                questionId);

            var lessonIds = await _mappingRepository.GetLessonsByQuestionAsync(
                questionId,
                ct);

            _logger.LogDebug(
                "Found {LessonCount} lessons for question {QuestionId}",
                lessonIds.Count(),
                questionId);

            return lessonIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching lessons for question {QuestionId}",
                questionId);
            return Enumerable.Empty<Guid>();
        }
    }

    public async Task<bool> TrackLearnMoreClickAsync(
        Guid playerId,
        Guid questionId,
        string context = "quiz-review",
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Tracking learn-more click for player {PlayerId} on question {QuestionId}",
                playerId,
                questionId);

            // Track the event in analytics
            await _analyticsService.TrackEventAsync(new AnalyticsEvent
            {
                EventType = "LearnMoreClick",
                PlayerId = playerId,
                EventData = new
                {
                    questionId = questionId,
                    context = context,
                    timestamp = DateTime.UtcNow
                }
            }, ct);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error tracking learn-more click for player {PlayerId}",
                playerId);
            return false;
        }
    }

    public async Task<List<LessonDto>> GetRecommendedLessonsAsync(
        Guid playerId,
        string? category = null,
        int? difficulty = null,
        int limit = 10,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Fetching recommended lessons for player {PlayerId}",
                playerId);

            // Clamp limit to reasonable range (1-50)
            var clampedLimit = Math.Max(1, Math.Min(50, limit));

            // TODO: Implement recommendation logic based on:
            // 1. Questions player answered incorrectly recently
            // 2. Topics where player has low performance
            // 3. Player's current skill level
            // 4. Popular lessons at their level

            var recommendedLessons = new List<LessonDto>();

            // For now, return empty list
            // This will be replaced with actual recommendation engine
            _logger.LogDebug(
                "Fetched {LessonCount} recommended lessons for player {PlayerId}",
                recommendedLessons.Count,
                playerId);

            return recommendedLessons;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching recommended lessons for player {PlayerId}",
                playerId);
            return new List<LessonDto>();
        }
    }
}
