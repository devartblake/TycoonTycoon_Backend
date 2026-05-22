using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Config;
using Tycoon.Backend.Application.Matchmaking;

namespace Tycoon.Backend.Api.Features.Matchmaking
{
    public static class MatchmakingEndpoints
    {
        public sealed record EnqueueRequest(Guid PlayerId, string Mode, int Tier);
        public sealed record CancelRequest(Guid PlayerId);
        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/matchmaking").WithTags("Matchmaking")
                .AddEndpointFilter(async (ctx, next) =>
                {
                    var flags = ctx.HttpContext.RequestServices.GetRequiredService<FeatureFlagService>();
                    if (!await flags.IsEnabledAsync("matchmaking_enabled", ctx.HttpContext.RequestAborted))
                        return Results.Json(new { error = new { code = "FeatureDisabled", message = "This feature is not available in the current release.", details = new { } } }, statusCode: StatusCodes.Status403Forbidden);
                    return await next(ctx);
                });

            g.MapPost("/enqueue", async (
                [FromBody] EnqueueRequest req,
                MatchmakingService mm,
                CancellationToken ct) =>
            {
                var res = await mm.EnqueueAsync(req.PlayerId, req.Mode, req.Tier, ct);

                if (res.Status == "Forbidden")
                    return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Player is not allowed to enter matchmaking.");

                // Optional semantic improvement: return 200 OK with the result
                return res.Status == "Queued"
                    ? Results.Accepted(value: res)
                    : Results.Ok(res);
            })
            // Optional: protect against spam enqueue/cancel requests from same player
            .RequireRateLimiting("matches-submit");

            g.MapPost("/cancel", async (
                [FromBody] CancelRequest req,
                MatchmakingService mm,
                CancellationToken ct) =>
            {
                await mm.CancelAsync(req.PlayerId, ct);
                return Results.NoContent();
            });

            g.MapGet("/status/{playerId:guid}", async (
                Guid playerId,
                MatchmakingService mm,
                CancellationToken ct) =>
            {
                var res = await mm.GetStatusAsync(playerId, ct);
                return Results.Ok(res);
            });

        }
    }
}
