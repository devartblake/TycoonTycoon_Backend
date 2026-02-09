using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Application.Powerups;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Powerups
{
    public static class PowerupsEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/powerups").WithTags("Powerups").WithOpenApi();

            g.MapGet("/state/{playerId:guid}", async ([FromRoute] Guid playerId, PowerupService svc, CancellationToken ct) =>
            {
                var res = await svc.GetStateAsync(playerId, ct);
                return Results.Ok(res);
            });

            g.MapPost("/use", async ([FromBody] UsePowerupRequest req, PowerupService svc, CancellationToken ct) =>
            {
                var res = await svc.UseAsync(req, ct);
                return Results.Ok(res);
            });
        }
    }
}
