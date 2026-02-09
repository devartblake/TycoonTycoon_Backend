using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Application.Skills;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminSkills
{
    public static class AdminSkillsEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/skills").WithTags("Admin/Skills").WithOpenApi();

            g.MapPost("/seed", async ([FromBody] SkillTreeCatalogDto req, SkillTreeService svc, CancellationToken ct) =>
            {
                var upserted = await svc.UpsertNodesAsync(req.Nodes, ct);
                return Results.Ok(new { upserted });
            });
        }
    }
}
