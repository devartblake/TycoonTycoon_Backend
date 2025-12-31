using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Application.Economy;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminEconomy
{
    public static class AdminEconomyEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/economy").WithTags("Admin/Economy");

            g.MapPost("/transactions", async ([FromBody] CreateEconomyTxnRequest req, EconomyService econ, CancellationToken ct) =>
            {
                var res = await econ.ApplyAsync(req, ct);
                return Results.Ok(res);
            });

            g.MapGet("/history/{playerId:guid}", async (
                [FromRoute] Guid playerId,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                EconomyService econ,
                CancellationToken ct) =>
            {
                var res = await econ.GetHistoryAsync(playerId, page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize, ct);
                return Results.Ok(res);
            });
        }
    }
}
