using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Seasons;

namespace Tycoon.Backend.Api.Features.AdminSeasons;

public static class AdminSeasonRewardsEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/seasons/rewards").WithTags("Admin/Seasons/Rewards").WithOpenApi();

        // Audit claims
        g.MapGet("/claims", async (
            [FromQuery] Guid? seasonId,
            [FromQuery] Guid? playerId,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IAppDb db,
            CancellationToken ct) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var q = db.SeasonRewardClaims.AsNoTracking();

            if (seasonId.HasValue) q = q.Where(x => x.SeasonId == seasonId.Value);
            if (playerId.HasValue) q = q.Where(x => x.PlayerId == playerId.Value);

            q = q.OrderByDescending(x => x.CreatedAtUtc);

            var total = await q.CountAsync(ct);

            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.SeasonId,
                    x.PlayerId,
                    x.EventId,
                    x.RewardDay,
                    x.AwardedCoins,
                    x.AwardedXp,
                    x.CreatedAtUtc
                })
                .ToListAsync(ct);

            return Results.Ok(new { page, pageSize, total, items });
        });

        // Force tier recompute (admin hook)
        g.MapPost("/recompute/{seasonId:guid}", async (
            Guid seasonId,
            TierAssignmentService tiers,
            CancellationToken ct) =>
        {
            await tiers.RecomputeAsync(seasonId, usersPerTier: 100, ct: ct);
            return Results.Ok(new { status = "ok" });
        });
    }
}
