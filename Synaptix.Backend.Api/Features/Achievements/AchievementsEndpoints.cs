using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Synaptix.Backend.Application.Achievements;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Achievements
{
    public static class AchievementsEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/achievements").WithTags("Achievements").RequireAuthorization();

            g.MapGet("", async (AchievementService svc, CancellationToken ct) =>
            {
                var res = await svc.GetCatalogAsync(ct);
                return Results.Ok(res);
            });

            g.MapGet("/player/{playerId:guid}", async ([FromRoute] Guid playerId, AchievementService svc, CancellationToken ct) =>
            {
                var res = await svc.GetPlayerAsync(playerId, ct);
                return Results.Ok(res);
            });

            g.MapPost("/unlock", async ([FromBody] UnlockAchievementRequest req, AchievementService svc, CancellationToken ct) =>
            {
                var res = await svc.UnlockAsync(req, ct);
                return Results.Ok(res);
            });
        }
    }
}
