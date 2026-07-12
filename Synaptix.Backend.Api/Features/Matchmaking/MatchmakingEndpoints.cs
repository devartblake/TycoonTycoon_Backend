using Microsoft.AspNetCore.Builder;
using Synaptix.Backend.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Config;
using Synaptix.Backend.Application.Matchmaking;

namespace Synaptix.Backend.Api.Features.Matchmaking
{
    public static class MatchmakingEndpoints
    {
        public sealed record EnqueueRequest(Guid PlayerId, string Mode, int Tier);
        public sealed record CancelRequest(Guid PlayerId);
        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/matchmaking").WithTags("Matchmaking")
                .RequireNotBanned();

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
