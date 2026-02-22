using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Application.Leaderboards;

namespace Tycoon.Backend.Api.Features.Mobile.Leaderboards
{
    public static class MobileLeaderboardsEndpoints
    {
        public static void Map(RouteGroupBuilder mobile)
        {
            var g = mobile.MapGroup("/leaderboards")
                .WithTags("Mobile/Leaderboards")
                .WithOpenApi();

            g.MapGet("/me/{playerId:guid}", async (Guid playerId, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new GetMyTier(playerId), ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            });

            g.MapGet("/tiers/{tierId:int}", async (
                int tierId,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var dto = await mediator.Send(new GetTierLeaderboard(tierId, page, pageSize), ct);
                return Results.Ok(dto);
            });
        }
    }
}
