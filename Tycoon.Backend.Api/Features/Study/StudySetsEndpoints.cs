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
    public static class StudySetsEndpoints
    {
        public static void Map(WebApplication app)
        {
            // Public study contract.
            // /study-sets is the dedicated backend surface for rehearsal-style study flows.
            var g = app.MapGroup("/study-sets").WithTags("StudySets");

            g.MapGet("", async (
                [FromQuery] Guid? playerId,
                [FromQuery] int? count,
                HttpContext httpContext,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var resolvedPlayerId = playerId ?? TryGetUserId(httpContext);
                var dto = await mediator.Send(new GetStudySets(resolvedPlayerId, count ?? 0), ct);
                return Results.Ok(dto);
            });

            g.MapGet("/recommended", async (
                [FromQuery] Guid? playerId,
                [FromQuery] int? count,
                HttpContext httpContext,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var resolvedPlayerId = playerId ?? TryGetUserId(httpContext);
                var dto = await mediator.Send(new GetRecommendedStudySets(resolvedPlayerId, count ?? 0), ct);
                return Results.Ok(dto);
            });

            g.MapPost("/favorites/{questionId:guid}", async (
                Guid questionId,
                HttpContext httpContext,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var playerId = TryGetUserId(httpContext);
                if (!playerId.HasValue)
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                var result = await mediator.Send(new AddStudyFavoriteQuestion(playerId.Value, questionId), ct);
                return result.Status switch
                {
                    "QuestionNotFound" => ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Question not found."),
                    _ => Results.Ok(result)
                };
            }).RequireAuthorization();

            g.MapDelete("/favorites/{questionId:guid}", async (
                Guid questionId,
                HttpContext httpContext,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var playerId = TryGetUserId(httpContext);
                if (!playerId.HasValue)
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                var result = await mediator.Send(new RemoveStudyFavoriteQuestion(playerId.Value, questionId), ct);
                return Results.Ok(result);
            }).RequireAuthorization();

            g.MapPost("", async (
                [FromBody] CreateStudySetRequest request,
                HttpContext httpContext,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var playerId = TryGetUserId(httpContext);
                if (!playerId.HasValue)
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                var dto = await mediator.Send(new CreateCustomStudySet(
                    playerId.Value,
                    request.Title,
                    request.Description,
                    request.QuestionIds), ct);

                return dto is null
                    ? ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "A custom study set requires a title and at least one approved question.")
                    : Results.Ok(dto);
            }).RequireAuthorization();

            g.MapPatch("/{id}", async (
                [FromRoute] string id,
                [FromBody] UpdateStudySetRequest request,
                HttpContext httpContext,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var playerId = TryGetUserId(httpContext);
                if (!playerId.HasValue)
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                if (!id.StartsWith("custom:", StringComparison.OrdinalIgnoreCase)
                    || !Guid.TryParse(id["custom:".Length..], out var studySetId))
                {
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Only custom study sets can be updated.");
                }

                var dto = await mediator.Send(new UpdateCustomStudySet(
                    playerId.Value,
                    studySetId,
                    request.Title,
                    request.Description,
                    request.QuestionIds), ct);

                return dto is null
                    ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Custom study set not found.")
                    : Results.Ok(dto);
            }).RequireAuthorization();

            g.MapGet("/{id}", async (
                [FromRoute] string id,
                [FromQuery] int? count,
                [FromQuery] Guid? playerId,
                HttpContext httpContext,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var resolvedPlayerId = playerId ?? TryGetUserId(httpContext);
                var dto = await mediator.Send(new GetStudySet(id, resolvedPlayerId, count ?? 0), ct);
                return dto is null
                    ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Study set not found.")
                    : Results.Ok(dto);
            });
        }

        private static Guid? TryGetUserId(HttpContext httpContext)
        {
            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                        ?? httpContext.User.FindFirst("sub");
            return claim is not null && Guid.TryParse(claim.Value, out var userId) && userId != Guid.Empty
                ? userId
                : null;
        }
    }
}
