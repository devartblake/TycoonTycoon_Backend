using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Quiz;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Quiz;

public static class QuizEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/quiz").WithTags("Quiz").RequireAuthorization();

        g.MapPost("/complete", async (
            [FromBody] CompleteQuizRequest req,
            HttpContext httpContext,
            IAppDb db,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (req.PlayerId == Guid.Empty || req.EventId == Guid.Empty)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId and eventId are required.");

            if (!TryGetAuthenticatedPlayerId(httpContext.User, out var jwtPlayerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            if (jwtPlayerId != req.PlayerId)
                return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Cannot complete a quiz for another player.");

            var answers = req.Answers?
                .Where(a => a.QuestionId != Guid.Empty && !string.IsNullOrWhiteSpace(a.SelectedOptionId))
                .ToArray() ?? [];
            if (answers.Length == 0)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "answers are required.");

            if (answers.Length > 50)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Maximum 50 answers per quiz completion.");

            var questionIds = answers.Select(a => a.QuestionId).Distinct().ToArray();
            if (questionIds.Length != answers.Length)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Duplicate question answers are not allowed.");

            var questions = await db.Questions
                .AsNoTracking()
                .Where(q => questionIds.Contains(q.Id))
                .ToDictionaryAsync(q => q.Id, ct);

            if (questions.Count != questionIds.Length)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "One or more questions were not found.");

            var correct = 0;
            var awardedXp = 0;
            var awardedCoins = 0;
            foreach (var answer in answers)
            {
                var question = questions[answer.QuestionId];
                var isCorrect = string.Equals(question.CorrectOptionId, answer.SelectedOptionId, StringComparison.OrdinalIgnoreCase);
                if (isCorrect)
                {
                    correct++;
                    var reward = RewardFor(question.Difficulty);
                    awardedXp += reward.Xp;
                    awardedCoins += reward.Coins;
                }
                else
                {
                    awardedXp += 1;
                }
            }

            var res = await mediator.Send(
                new CompleteQuiz(req.PlayerId, req.EventId, correct, answers.Length, awardedXp, awardedCoins), ct);

            return Results.Ok(res);
        }).RequireRateLimiting("matches-submit");
    }

    public sealed record CompleteQuizRequest(
        Guid PlayerId,
        Guid EventId,
        IReadOnlyList<QuizAnswerSubmission>? Answers = null,
        int? Score = null,
        int? TotalQuestions = null,
        string? Category = null
    );

    public sealed record QuizAnswerSubmission(
        Guid QuestionId,
        string SelectedOptionId,
        int? AnswerTimeMs = null
    );

    private static (int Xp, int Coins) RewardFor(QuestionDifficulty difficulty) =>
        difficulty switch
        {
            QuestionDifficulty.Easy => (8, 3),
            QuestionDifficulty.Medium => (10, 4),
            QuestionDifficulty.Hard => (14, 5),
            QuestionDifficulty.Expert => (20, 8),
            _ => (8, 3)
        };

    private static bool TryGetAuthenticatedPlayerId(ClaimsPrincipal user, out Guid playerId)
    {
        playerId = Guid.Empty;
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return raw is not null && Guid.TryParse(raw, out playerId);
    }
}
