using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Synaptix.Backend.Application.Features.LearningHub;
using Synaptix.Shared.Contracts.Dtos.LearningHub;

namespace Synaptix.Backend.Api.Features.LearningHub;

/// <summary>
/// API endpoints for learning hub features.
/// Enables linking quiz questions to educational resources.
/// </summary>
public static class LearningHubEndpoints
{
    /// <summary>
    /// Register all learning hub endpoints.
    /// </summary>
    public static void MapLearningHubRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/learning-hub")
            .WithTags("Learning Hub");

        // Public endpoints
        group.MapGet("/questions/{questionId}/lessons", GetLessonsForQuestion)
            .WithName("GetLessonsForQuestion")
            .WithOpenApi()
            .Produces<List<Guid>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/quiz-review/learn-more-click", TrackLearnMoreClick)
            .WithName("TrackLearnMoreClick")
            .WithOpenApi()
            .Produces<LearnMoreClickResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapGet("/recommended-lessons", GetRecommendedLessons)
            .WithName("GetRecommendedLessons")
            .WithOpenApi()
            .Produces<RecommendedLessonsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }

    /// <summary>
    /// Get all lessons linked to a specific question.
    /// Used when a user gets a question wrong and wants to learn more.
    /// </summary>
    /// <param name="questionId">The question ID to find lessons for</param>
    /// <param name="service">The learning hub service</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of lesson IDs linked to the question</returns>
    private static async Task<IResult> GetLessonsForQuestion(
        Guid questionId,
        ILearningHubService service,
        CancellationToken ct)
    {
        if (questionId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Invalid question ID" });
        }

        var lessonIds = await service.GetLessonsForQuestionAsync(questionId, ct);

        if (!lessonIds.Any())
        {
            return Results.NotFound(new { message = "No lessons found for this question" });
        }

        return Results.Ok(lessonIds);
    }

    /// <summary>
    /// Track when a user clicks "Learn More" from the quiz review screen.
    /// This helps us understand engagement with learning resources.
    /// </summary>
    /// <param name="request">The learn-more click request</param>
    /// <param name="userContext">The current user context</param>
    /// <param name="service">The learning hub service</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response indicating success/failure</returns>
    private static async Task<IResult> TrackLearnMoreClick(
        LearnMoreClickRequest request,
        ILearningHubService service,
        CancellationToken ct)
    {
        if (request.QuestionId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Invalid question ID" });
        }

        // Use a default player ID (would normally come from authenticated user)
        var playerId = Guid.NewGuid();

        var success = await service.TrackLearnMoreClickAsync(
            playerId,
            request.QuestionId,
            request.Context,
            ct);

        if (!success)
        {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Results.Ok(new LearnMoreClickResponse
        {
            Success = true,
            Message = "Click tracked successfully",
            RecordedAt = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Get recommended lessons based on the player's quiz performance.
    /// Returns lessons for topics where the player has struggled recently.
    /// </summary>
    /// <param name="category">Optional topic category filter</param>
    /// <param name="difficulty">Optional difficulty level filter (1-5)</param>
    /// <param name="limit">Max number of lessons to return (1-50, default 10)</param>
    /// <param name="service">The learning hub service</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Recommended lessons for the player</returns>
    private static async Task<IResult> GetRecommendedLessons(
        string? category,
        int? difficulty,
        int? limit,
        ILearningHubService service,
        CancellationToken ct)
    {
        // Use a default player ID (would normally come from authenticated user)
        var playerId = Guid.NewGuid();

        // Validate inputs
        if (difficulty.HasValue && (difficulty < 1 || difficulty > 5))
        {
            return Results.BadRequest(new { error = "Difficulty must be between 1 and 5" });
        }

        var lessonLimit = limit ?? 10;
        if (lessonLimit < 1 || lessonLimit > 50)
        {
            return Results.BadRequest(new { error = "Limit must be between 1 and 50" });
        }

        var lessons = await service.GetRecommendedLessonsAsync(
            playerId,
            category,
            difficulty,
            lessonLimit,
            ct);

        var response = new RecommendedLessonsResponse
        {
            Lessons = lessons,
            TotalCount = lessons.Count,
            RecommendationReason = "based-on-quiz-performance"
        };

        return Results.Ok(response);
    }
}
