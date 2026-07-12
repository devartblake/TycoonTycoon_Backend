using Microsoft.AspNetCore.Builder;
using Synaptix.Backend.Api.Security;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Application.Config;
using Synaptix.Backend.Application.Skills;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Skills
{
    public static class SkillsEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/skills").WithTags("Skills")
                .RequireNotBanned();

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

            g.MapPost("/use", async ([FromBody] UseSkillRequest req, SkillTreeService svc, CancellationToken ct) =>
            {
                var res = await svc.UseAsync(req, ct);
                return Results.Ok(res);
            });
        }
    }
}
