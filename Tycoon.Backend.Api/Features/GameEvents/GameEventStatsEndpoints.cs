using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.EventStats;

namespace Tycoon.Backend.Api.Features.GameEvents
{
    public static class GameEventStatsEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/game-events").WithTags("GameEventStats").WithOpenApi();

            // Ranked leaderboard for a specific closed game event
            g.MapGet("/{gameEventId:guid}/leaderboard", async (
                [FromRoute] Guid gameEventId,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(
                    new GetGameEventLeaderboard(gameEventId, page < 1 ? 1 : page, pageSize < 1 ? 50 : pageSize), ct);
                return Results.Ok(res);
            });

            // Player's event participation history
            g.MapGet("/players/{playerId:guid}/event-history", async (
                [FromRoute] Guid playerId,
                [FromQuery] Guid? seasonId,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(
                    new GetPlayerEventHistory(playerId, seasonId, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(res);
            }).RequireAuthorization();

            // Season-wide event champion leaderboard
            g.MapGet("/season-leaderboard", async (
                [FromQuery] Guid seasonId,
                [FromQuery] string? sortBy,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                IMediator mediator,
                CancellationToken ct) =>
            {
                if (seasonId == Guid.Empty)
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "BAD_REQUEST", "seasonId is required.");

                var res = await mediator.Send(
                    new GetEventSeasonLeaderboard(seasonId, sortBy ?? "event_wins", page < 1 ? 1 : page, pageSize < 1 ? 50 : pageSize), ct);
                return Results.Ok(res);
            });
        }

        public static void MapTerritory(WebApplication app)
        {
            var g = app.MapGroup("/territory").WithTags("TerritoryStats").WithOpenApi();

            // Territory dominance leaderboard for a tier
            g.MapGet("/{seasonId:guid}/{tierNumber:int}/dominance", async (
                [FromRoute] Guid seasonId,
                [FromRoute] int tierNumber,
                [FromQuery] int top,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(
                    new GetTerritoryDominanceLeaderboard(seasonId, tierNumber, top < 1 ? 20 : top), ct);
                return Results.Ok(res);
            });
        }
    }
}
