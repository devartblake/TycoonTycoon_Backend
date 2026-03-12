using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Seasons;

namespace Tycoon.Backend.Api.Features.AdminSeasons;

public static class AdminSeasonLifecycleEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/seasons").WithTags("Admin/Seasons").WithOpenApi();

        g.MapPost("/{seasonId:guid}/close", async (
            Guid seasonId,
            SeasonCloseOrchestrator orch,
            CancellationToken ct) =>
        {
            var status = await orch.CloseAsync(seasonId, ct);

            return status switch
            {
                "Closed" => Results.Ok(new { status }),
                "AlreadyClosed" => Results.Ok(new { status }),
                "NotActive" => ApiResponses.Error(StatusCodes.Status409Conflict, "CONFLICT", "Season is not active."),
                "NotFound" => ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Season not found."),
                _ => ApiResponses.Error(StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", $"Unexpected status: {status}")
            };
        });
    }
}
