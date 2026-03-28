using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Seasons;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminSeasons
{
    public static class AdminSeasonPointsEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/season-points").WithTags("Admin/SeasonPoints").WithOpenApi();

            g.MapPost("/transactions", async ([FromBody] ApplySeasonPointsRequest req, SeasonPointsService svc, CancellationToken ct) =>
            {
                if (req.SeasonId == Guid.Empty)
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "SeasonId is required.");

                if (req.PlayerId == Guid.Empty)
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "PlayerId is required.");

                if (string.IsNullOrWhiteSpace(req.Kind))
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Kind is required.");

                var res = await svc.ApplyAsync(req, ct);
                return Results.Ok(res);
            });

            g.MapGet("/history/{playerId:guid}", async (
                [FromRoute] Guid playerId,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                SeasonPointsService svc,
                CancellationToken ct) =>
            {
                var res = await svc.GetHistoryAsync(playerId, page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize, ct);
                return Results.Ok(res);
            });
        }
    }
}
