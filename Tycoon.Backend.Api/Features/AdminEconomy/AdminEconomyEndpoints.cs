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
            var g = admin.MapGroup("/economy").WithTags("Admin/Economy").WithOpenApi();

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

            //g.MapPost("/rollback", async (
            //    [FromBody] AdminRollbackEconomyRequest req,
            //    EconomyService econ,
            //    CancellationToken ct) =>
            //{
            //    if (req.EventId == Guid.Empty)
            //        return Results.BadRequest("EventId is required.");

            //    if (string.IsNullOrWhiteSpace(req.Reason))
            //        return Results.BadRequest("Reason is required.");

            //    try
            //    {
            //        var res = await econ.RollbackByEventIdAsync(req.EventId, req.Reason.Trim(), ct);
            //        return Results.Ok(res);
            //    }
            //    catch (InvalidOperationException ex)
            //    {
            //        // Align to your existing patterns: deterministic admin failures.
            //        // - not found => 404
            //        // - already rolled back => 409
            //        var msg = ex.Message ?? "Rollback failed.";

            //        if (msg.Contains("not found", StringComparison.OrdinalIgnoreCase))
            //            return Results.NotFound(msg);

            //        if (msg.Contains("already rolled back", StringComparison.OrdinalIgnoreCase))
            //            return Results.Conflict(msg);

            //        return Results.BadRequest(msg);
            //    }
            //});
        }
    }
}
