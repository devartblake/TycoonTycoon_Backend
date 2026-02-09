using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Application.Skills;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Skills
{
    public static class SkillsEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/skills").WithTags("Skills").WithOpenApi();

            g.MapGet("/tree", async (SkillTreeService svc, CancellationToken ct) =>
            {
                var res = await svc.GetCatalogAsync(ct);
                return Results.Ok(res);
            });

            g.MapGet("/state/{playerId:guid}", async ([FromRoute] Guid playerId, SkillTreeService svc, CancellationToken ct) =>
            {
                var res = await svc.GetStateAsync(playerId, ct);
                return Results.Ok(res);
            });

            g.MapPost("/unlock", async ([FromBody] UnlockSkillRequest req, SkillTreeService svc, CancellationToken ct) =>
            {
                var res = await svc.UnlockAsync(req, ct);
                return Results.Ok(res);
            });

            g.MapPost("/respec", async ([FromBody] RespecSkillsRequest req, SkillTreeService svc, CancellationToken ct) =>
            {
                var res = await svc.RespecAsync(req, ct);
                return Results.Ok(res);
            });
        }
    }
}
