using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Territory;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Territory
{
    public static class TerritoryEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/territory").WithTags("Territory");

            g.MapGet("/{seasonId:guid}/{tierNumber:int}", async (
                [FromRoute] Guid seasonId,
                [FromRoute] int tierNumber,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(new GetTerritoryBoard(seasonId, tierNumber), ct);
                return Results.Ok(res);
            });

            g.MapPost("/duel", async (
                [FromBody] StartTerritoryDuelRequest req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(
                    new StartTerritoryDuel(req.EventId, req.SeasonId, req.TierNumber, req.Category, req.ChallengerId), ct);

                return res.Status switch
                {
                    "FeatureDisabled" => ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "FEATURE_DISABLED", "Territory feature is currently disabled."),
                    _ when res.MatchId == Guid.Empty => Results.Ok(new { status = "AlreadyOwner", matchId = Guid.Empty, tileOwnerId = res.TileOwnerId }),
                    _ => Results.Ok(res)
                };
            }).RequireAuthorization();

            g.MapGet("/multiplier/{seasonId:guid}/{tierNumber:int}/{playerId:guid}", async (
                [FromRoute] Guid seasonId,
                [FromRoute] int tierNumber,
                [FromRoute] Guid playerId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var multiplierBps = await mediator.Send(new GetPlayerTileMultiplier(seasonId, tierNumber, playerId), ct);
                return Results.Ok(new { totalMultiplierBps = multiplierBps });
            });
        }
    }
}
