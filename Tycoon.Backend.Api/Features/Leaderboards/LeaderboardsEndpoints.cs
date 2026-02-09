using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Application.Leaderboards;

namespace Tycoon.Backend.Api.Features.Leaderboards
{
    public static class LeaderboardsEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/leaderboards").WithTags("Leaderboards").WithOpenApi();

            // Existing: keep for now (until auth-sub binding is enforced)
            g.MapGet("/me/{playerId:guid}", async (Guid playerId, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new GetMyTier(playerId), ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            });

            // NEW: Tier leaderboard (paged)
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

            // NEW: Admin trigger recalculation immediately
            // If you prefer: wrap with your existing ops key middleware / admin auth policy.
            g.MapPost("/recalc", async (IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new RecalculateLeaderboard(), ct);
                return Results.Ok(result);
            });
        }
    }
}
