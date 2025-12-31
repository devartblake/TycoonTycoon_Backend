using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Seasons;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminSeasons
{
    public static class AdminSeasonsEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/seasons").WithTags("Admin/Seasons");

            g.MapGet("", async ([FromQuery] int page, [FromQuery] int pageSize, SeasonService svc, CancellationToken ct) =>
            {
                var res = await svc.ListAsync(page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize, ct);
                return Results.Ok(res);
            });

            g.MapPost("", async ([FromBody] CreateSeasonRequest req, SeasonService svc, CancellationToken ct) =>
            {
                var created = await svc.CreateAsync(req, ct);
                return Results.Ok(created);
            });

            g.MapPost("/activate", async ([FromBody] ActivateSeasonRequest req, SeasonService svc, CancellationToken ct) =>
            {
                var activated = await svc.ActivateAsync(req, ct);
                return activated is null ? Results.NotFound() : Results.Ok(activated);
            });

            g.MapPost("/close", async ([FromBody] CloseSeasonRequest req, SeasonService svc, CancellationToken ct) =>
            {
                var (closed, next) = await svc.CloseAsync(req, ct);
                if (closed is null) return Results.NotFound();
                return Results.Ok(new { closed, next });
            });

            g.MapPost("/{seasonId:guid}/recompute-tiers", async (
                [FromRoute] Guid seasonId,
                TierAssignmentService tiers,
                CancellationToken ct) =>
            {
                await tiers.RecomputeAsync(seasonId, usersPerTier: 100, ct: ct);
                return Results.Ok(new { status = "ok" });
            });

            g.MapGet("/{seasonId:guid}/leaderboard", async (
                [FromRoute] Guid seasonId,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                [FromQuery] int? tier,
                IAppDb db,
                CancellationToken ct) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var q = db.PlayerSeasonProfiles.AsNoTracking().Where(x => x.SeasonId == seasonId);

                if (tier.HasValue)
                    q = q.Where(x => x.Tier == tier.Value);

                q = q.OrderBy(x => x.SeasonRank);

                var total = await q.CountAsync(ct);

                var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(x => new SeasonLeaderboardItemDto(
                        x.PlayerId,
                        x.RankPoints,
                        x.Wins,
                        x.Losses,
                        x.Draws,
                        x.Tier,
                        x.TierRank,
                        x.SeasonRank
                    ))
                    .ToListAsync(ct);

                return Results.Ok(new SeasonLeaderboardResponseDto(page, pageSize, total, items));
            });
        }
    }
}
