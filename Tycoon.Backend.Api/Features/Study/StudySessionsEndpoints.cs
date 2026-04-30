using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Study;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Study
{
    public static class StudySessionsEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/study-sessions")
                .WithTags("StudySessions")
                
                .RequireAuthorization();

            g.MapPost("", async (
                [FromBody] CreateStudySessionRequest request,
                HttpContext httpContext,
                IMediator mediator,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(httpContext, out var playerId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                if (string.IsNullOrWhiteSpace(request.StudySetId))
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "studySetId is required.");

                var dto = await mediator.Send(new CreateStudySession(playerId, request.StudySetId, request.Mode, request.Count), ct);
                return dto is null
                    ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Study set not found.")
                    : Results.Ok(dto);
            });

            g.MapPost("/{id:guid}/progress", async (
                Guid id,
                [FromBody] UpdateStudySessionProgressRequest request,
                HttpContext httpContext,
                IMediator mediator,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(httpContext, out var playerId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                var result = await mediator.Send(new UpdateStudySessionProgress(
                    id,
                    playerId,
                    request.QuestionId,
                    request.SelectedOptionId,
                    request.CurrentQuestionIndex,
                    request.FlashcardAction,
                    request.Confidence,
                    request.AnswerRevealed,
                    request.IsCompleted), ct);

                return result.Status switch
                {
                    "NotFound" => ApiResponses.Error(StatusCodes.Status404NotFound, result.ErrorCode!, result.ErrorMessage!),
                    "ValidationError" => ApiResponses.Error(StatusCodes.Status400BadRequest, result.ErrorCode!, result.ErrorMessage!),
                    _ => Results.Ok(result.Session)
                };
            });

            g.MapGet("/{id:guid}/summary", async (
                Guid id,
                HttpContext httpContext,
                IMediator mediator,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(httpContext, out var playerId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                var dto = await mediator.Send(new GetStudySessionSummary(id, playerId), ct);
                return dto is null
                    ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Study session not found.")
                    : Results.Ok(dto);
            });
        }

        private static bool TryGetUserId(HttpContext httpContext, out Guid userId)
        {
            userId = Guid.Empty;
            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                        ?? httpContext.User.FindFirst("sub");
            return claim is not null && Guid.TryParse(claim.Value, out userId) && userId != Guid.Empty;
        }
    }
}
