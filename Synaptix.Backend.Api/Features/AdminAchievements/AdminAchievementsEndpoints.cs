using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Synaptix.Backend.Application.Achievements;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.AdminAchievements
{
    public static class AdminAchievementsEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/achievements").WithTags("AdminAchievements");

            g.MapPost("/seed", async ([FromBody] AchievementCatalogDto req, AchievementService svc, CancellationToken ct) =>
            {
                var upserted = await svc.UpsertAsync(req.Achievements, ct);
                return Results.Ok(new { upserted });
            });
        }
    }
}
