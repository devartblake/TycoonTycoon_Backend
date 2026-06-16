using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Territory;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Territory
{
    public static class TerritoryEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
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
                    "FeatureDisabled" => ApiResponses.Error(StatusCodes.Status403Forbidden, "FeatureDisabled", "This feature is not available in the current release."),
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
