using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Application.Seasons;

namespace Tycoon.Backend.Api.Features.AdminSeasons;

public static class AdminSeasonLifecycleEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/seasons").WithTags("Admin/Seasons");

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
                "NotActive" => Results.Conflict(new { status }),
                "NotFound" => Results.NotFound(),
                _ => Results.Problem("Unexpected status: " + status)
            };
        });
    }
}
