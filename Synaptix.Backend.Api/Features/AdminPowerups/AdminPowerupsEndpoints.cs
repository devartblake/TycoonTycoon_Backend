using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Synaptix.Backend.Application.Powerups;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.AdminPowerups
{
    public static class AdminPowerupsEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/powerups").WithTags("Admin/Powerups");

            g.MapPost("/grant", async ([FromBody] GrantPowerupRequest req, PowerupService svc, CancellationToken ct) =>
            {
                var res = await svc.GrantAsync(req, ct);
                return Results.Ok(res);
            });

            g.MapGet("/state/{playerId:guid}", async ([FromRoute] Guid playerId, PowerupService svc, CancellationToken ct) =>
            {
                var res = await svc.GetStateAsync(playerId, ct);
                return Results.Ok(res);
            });
        }
    }
}
