using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Synaptix.Backend.Application.Leaderboards;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Leaderboards
{
    public static class ArcadeLeaderboardEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/leaderboards/arcade").WithTags("Leaderboards");

            g.MapPost("/submit", SubmitScore)
                .WithName("SubmitArcadeScore")
                .RequireAuthorization();

            g.MapGet("/{gameId}/{difficulty}", GetLeaderboard)
                .WithName("GetArcadeLeaderboard");
        }

        private static async Task<IResult> SubmitScore(
            [FromBody] ArcadeScoreSubmitRequest request,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken ct)
        {
            // Get player ID from JWT
            var subject = httpContext.User.FindFirst("sub")?.Value
                          ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (subject is null || !Guid.TryParse(subject, out var playerId))
                return Results.Unauthorized();

            // Validate request
            if (string.IsNullOrWhiteSpace(request.GameId))
                return Results.BadRequest(new { error = "GameId is required" });
            if (string.IsNullOrWhiteSpace(request.Difficulty))
                return Results.BadRequest(new { error = "Difficulty is required" });
            if (request.Score < 0)
                return Results.BadRequest(new { error = "Score cannot be negative" });
            if (request.DurationMs < 0)
                return Results.BadRequest(new { error = "DurationMs cannot be negative" });

            try
            {
                var submitted = await mediator.Send(
                    new SubmitArcadeScore(
                        playerId,
                        request.GameId,
                        request.Difficulty,
                        request.Score,
                        request.DurationMs),
                    ct);

                if (!submitted)
                    return Results.Ok(new { success = false, message = "Score was not a personal best" });

                return Results.Ok(new { success = true, message = "Score submitted successfully" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = "submit_failed", message = ex.Message });
            }
        }

        private static async Task<IResult> GetLeaderboard(
            [FromRoute] string gameId,
            [FromRoute] string difficulty,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(gameId))
                return Results.BadRequest(new { error = "gameId is required" });
            if (string.IsNullOrWhiteSpace(difficulty))
                return Results.BadRequest(new { error = "difficulty is required" });

            try
            {
                // Get player ID if authenticated (optional)
                Guid? playerId = null;
                var subject = httpContext.User.FindFirst("sub")?.Value
                              ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (subject is not null && Guid.TryParse(subject, out var userId))
                    playerId = userId;

                var response = await mediator.Send(
                    new GetArcadeLeaderboard(gameId, difficulty, page, pageSize, playerId),
                    ct);

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = "fetch_failed", message = ex.Message });
            }
        }
    }
}
