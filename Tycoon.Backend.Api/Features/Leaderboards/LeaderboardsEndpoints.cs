using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Leaderboards;

namespace Tycoon.Backend.Api.Features.Leaderboards
{
    public static class LeaderboardsEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/leaderboards").WithTags("Leaderboards").WithOpenApi();

            // Frontend compatibility route(s): some clients call singular /leaderboard.
            MapLegacyLeaderboard(app.MapGroup("/leaderboard").WithTags("Leaderboards").WithOpenApi());
            MapLegacyLeaderboard(app.MapGroup("/api/v1/leaderboard").WithTags("Leaderboards").WithOpenApi());

            // Existing: keep for now (until auth-sub binding is enforced)
            g.MapGet("/me/{playerId:guid}", async (Guid playerId, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new GetMyTier(playerId), ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            });

            // NEW: Tier leaderboard (paged)
            g.MapGet("/tiers/{tierId:int}", async (
                int tierId,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var dto = await mediator.Send(new GetTierLeaderboard(tierId, page, pageSize), ct);
                return Results.Ok(dto);
            });

            // NEW: Admin trigger recalculation immediately
            g.MapPost("/recalc", async (IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new RecalculateLeaderboard(), ct);
                return Results.Ok(result);
            });
        }


        private sealed record LegacyLeaderboardRow(
            Guid PlayerId,
            string Username,
            int Score,
            int Rank,
            int Tier,
            int TierRank,
            int Level);

        private static void MapLegacyLeaderboard(RouteGroupBuilder legacy)
        {
            legacy.MapGet("", async (
                [FromQuery] int limit,
                IAppDb db,
                ILoggerFactory loggerFactory,
                CancellationToken ct) =>
            {
                var logger = loggerFactory.CreateLogger("Leaderboards.Legacy");
                var take = limit <= 0 ? 100 : Math.Min(limit, 500);

                List<LegacyLeaderboardRow> rows;
                try
                {
                    rows = await (
                        from e in db.LeaderboardEntries.AsNoTracking()
                        join p in db.Players.AsNoTracking() on e.PlayerId equals p.Id
                        orderby e.GlobalRank ascending
                        select new
                        {
                            e.PlayerId,
                            p.Username,
                            e.Score,
                            Rank = e.GlobalRank,
                            Tier = e.TierId,
                            e.TierRank,
                            p.Level
                        })
                        .Take(take)
                                                .Select(x => new LegacyLeaderboardRow(
                            x.PlayerId,
                            x.Username,
                            x.Score,
                            x.Rank,
                            x.Tier,
                            x.TierRank,
                            x.Level))
                        .ToListAsync(ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Legacy leaderboard query failed; falling back to players snapshot.");

                    var fallbackPlayers = await db.Players.AsNoTracking()
                        .OrderByDescending(x => x.Score)
                        .ThenBy(x => x.Id)
                        .Take(take)
                        .Select(x => new
                        {
                            PlayerId = x.Id,
                            Username = x.Username,
                            Score = x.Score,
                            Rank = 0,
                            Tier = 0,
                            TierRank = 0,
                            x.Level
                        })
                        .ToListAsync(ct);

                    rows = fallbackPlayers
                        .Select((x, i) => new
                        {
                            x.PlayerId,
                            x.Username,
                            x.Score,
                            Rank = i + 1,
                            x.Tier,
                            x.TierRank,
                            x.Level
                        })
                        .Select(x => new LegacyLeaderboardRow(
                            x.PlayerId,
                            x.Username,
                            x.Score,
                            x.Rank,
                            x.Tier,
                            x.TierRank,
                            x.Level))
                        .ToList();
                }

                var payload = rows.Select(x => new Dictionary<string, object?>
                {
                    ["user_id"] = x.PlayerId,
                    ["playerName"] = x.Username,
                    ["score"] = x.Score,
                    ["rank"] = x.Rank,
                    ["tier"] = x.Tier,
                    ["tierRank"] = x.TierRank,
                    ["wins"] = 0,
                    ["avatar"] = null,
                    ["level"] = x.Level
                });

                return Results.Ok(payload);
            });
        }
    }
}
