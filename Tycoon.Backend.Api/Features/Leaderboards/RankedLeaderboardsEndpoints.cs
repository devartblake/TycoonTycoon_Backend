using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Application.Leaderboards;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Leaderboards;

public static class RankedLeaderboardsEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/leaderboards/ranked").WithTags("Leaderboards/Ranked").WithOpenApi();

        g.MapGet("", async (
            [FromQuery] Guid? seasonId,
            [FromQuery] string scope,
            [FromQuery] int? tier,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] string sort,
            RankedLeaderboardService svc,
            CancellationToken ct) =>
        {
            var dto = await svc.GetAsync(new RankedLeaderboardQueryDto(
                SeasonId: seasonId,
                Scope: string.IsNullOrWhiteSpace(scope) ? "global" : scope,
                Tier: tier,
                Page: page <= 0 ? 1 : page,
                PageSize: pageSize <= 0 ? 25 : pageSize,
                Sort: string.IsNullOrWhiteSpace(sort) ? "points" : sort
            ), ct);

            return Results.Ok(dto);
        });
    }
}
