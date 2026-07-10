using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Seasons;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Seasons
{
    public static class SeasonsEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/seasons").WithTags("Seasons");

            g.MapGet("/active", async (SeasonService svc, CancellationToken ct) =>
            {
                var s = await svc.GetActiveAsync(ct);
                return s is null ? Results.NotFound() : Results.Ok(s);
            });

            g.MapGet("/state/{playerId:guid}", async (
                [FromRoute] Guid playerId,
                SeasonService seasons,
                IAppDb db,
                CancellationToken ct) =>
            {
                var active = await seasons.GetActiveAsync(ct);
                if (active is null) return Results.NotFound();

                var profile = await db.PlayerSeasonProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SeasonId == active.SeasonId && x.PlayerId == playerId, ct);

                if (profile is null)
                {
                    return Results.Ok(new PlayerSeasonStateDto(playerId, active.SeasonId, 0, 0, 0, 0, 0, 1, 0, 0));
                }

                return Results.Ok(new PlayerSeasonStateDto(
                    profile.PlayerId,
                    profile.SeasonId,
                    profile.RankPoints,
                    profile.Wins,
                    profile.Losses,
                    profile.Draws,
                    profile.MatchesPlayed,
                    profile.Tier,
                    profile.TierRank,
                    profile.SeasonRank
                ));
            });

            // GET /seasons/active/leaderboard — standings for the active season.
            g.MapGet("/active/leaderboard", async (
                HttpContext httpContext,
                SeasonService seasons,
                IAppDb db,
                CancellationToken ct,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 50) =>
            {
                var active = await seasons.GetActiveAsync(ct);
                if (active is null)
                    return Results.NotFound();

                return await BuildLeaderboardAsync(active.SeasonId, page, pageSize, httpContext, db, ct);
            });

            // GET /seasons/tiebreakers/mine — the caller's pending tiebreakers.
            g.MapGet("/tiebreakers/mine", async (
                HttpContext httpContext,
                SeasonTiebreakerService tiebreakers,
                CancellationToken ct) =>
            {
                var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                            ?? httpContext.User.FindFirst("sub");
                if (claim is null || !Guid.TryParse(claim.Value, out var playerId) || playerId == Guid.Empty)
                    return Results.Unauthorized();

                var items = await tiebreakers.GetPendingForPlayerAsync(playerId, ct);
                return Results.Ok(new SeasonTiebreakerListResponseDto(
                    items.Count, items.Select(ToTiebreakerDto).ToList()));
            }).RequireAuthorization();

            // GET /seasons/{seasonId}/tiebreakers — read-only tiebreaker status.
            g.MapGet("/{seasonId:guid}/tiebreakers", async (
                [FromRoute] Guid seasonId,
                IAppDb db,
                CancellationToken ct) =>
            {
                var items = await db.SeasonTiebreakers.AsNoTracking()
                    .Where(x => x.SeasonId == seasonId)
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .ToListAsync(ct);
                return Results.Ok(new SeasonTiebreakerListResponseDto(
                    items.Count, items.Select(ToTiebreakerDto).ToList()));
            });

            // GET /seasons/{seasonId}/leaderboard — standings for any season.
            // Closed seasons serve the immutable rank snapshot; live seasons
            // serve the current profiles.
            g.MapGet("/{seasonId:guid}/leaderboard", async (
                [FromRoute] Guid seasonId,
                HttpContext httpContext,
                IAppDb db,
                CancellationToken ct,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 50) =>
            {
                return await BuildLeaderboardAsync(seasonId, page, pageSize, httpContext, db, ct);
            });
        }

        private static SeasonTiebreakerDto ToTiebreakerDto(SeasonTiebreaker x) => new(
            x.Id, x.SeasonId, x.Scope, x.Tier, x.BoundaryRank, x.RankPoints,
            x.PlayerIds, x.ScheduledAtUtc, x.ExpiresAtUtc, x.Status,
            x.MatchId, x.WinnerPlayerId, x.CreatedAtUtc, x.ResolvedAtUtc);

        private static async Task<IResult> BuildLeaderboardAsync(
            Guid seasonId,
            int page,
            int pageSize,
            HttpContext httpContext,
            IAppDb db,
            CancellationToken ct)
        {
            var season = await db.Seasons.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == seasonId, ct);
            if (season is null)
                return Results.NotFound();

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : Math.Clamp(pageSize, 1, 100);

            // Snapshot rows exist only after a season closes; their presence
            // makes them the source of truth for final standings.
            var hasSnapshot = await db.SeasonRankSnapshots.AsNoTracking()
                .AnyAsync(x => x.SeasonId == seasonId, ct);

            // Project both sources into one shape so ranking/paging is shared.
            // Member-init bindings (not constructor args) so EF can compose
            // the ordering/paging operators over the projection.
            var rows = hasSnapshot
                ? db.SeasonRankSnapshots.AsNoTracking()
                    .Where(x => x.SeasonId == seasonId)
                    .Select(x => new LeaderboardRow
                    {
                        PlayerId = x.PlayerId,
                        RankPoints = x.RankPoints,
                        Wins = x.Wins,
                        Losses = x.Losses,
                        Draws = x.Draws,
                        MatchesPlayed = x.MatchesPlayed,
                        Tier = x.Tier,
                        TierRank = x.TierRank,
                    })
                : db.PlayerSeasonProfiles.AsNoTracking()
                    .Where(x => x.SeasonId == seasonId)
                    .Select(x => new LeaderboardRow
                    {
                        PlayerId = x.PlayerId,
                        RankPoints = x.RankPoints,
                        Wins = x.Wins,
                        Losses = x.Losses,
                        Draws = x.Draws,
                        MatchesPlayed = x.MatchesPlayed,
                        Tier = x.Tier,
                        TierRank = x.TierRank,
                    });

            // Deterministic standings: points, then wins, then fewest matches
            // (efficiency), then PlayerId for stability.
            var ordered = rows
                .OrderByDescending(x => x.RankPoints)
                .ThenByDescending(x => x.Wins)
                .ThenBy(x => x.MatchesPlayed)
                .ThenBy(x => x.PlayerId);

            var total = await ordered.CountAsync(ct);
            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            var pageRows = await ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var playerIds = pageRows.Select(x => x.PlayerId).ToList();

            // Optional: include the caller's own row even when off-page.
            Guid callerId = Guid.Empty;
            var callerClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                              ?? httpContext.User.FindFirst("sub");
            if (callerClaim is not null && Guid.TryParse(callerClaim.Value, out var parsed))
            {
                callerId = parsed;
                if (!playerIds.Contains(callerId))
                    playerIds.Add(callerId);
            }

            var users = await db.Users.AsNoTracking()
                .Where(u => playerIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Handle, u.AvatarUrl })
                .ToDictionaryAsync(u => u.Id, ct);

            var startRank = (page - 1) * pageSize;
            var items = pageRows.Select((row, index) =>
            {
                users.TryGetValue(row.PlayerId, out var user);
                return new PublicSeasonLeaderboardEntryDto(
                    startRank + index + 1,
                    row.PlayerId,
                    user?.Handle ?? "unknown",
                    user?.Handle ?? "Unknown",
                    user?.AvatarUrl,
                    row.RankPoints,
                    row.Wins,
                    row.Losses,
                    row.Draws,
                    row.Tier,
                    row.TierRank);
            }).ToList();

            PublicSeasonLeaderboardEntryDto? me = null;
            if (callerId != Guid.Empty)
            {
                me = items.FirstOrDefault(x => x.PlayerId == callerId);
                if (me is null)
                {
                    var mine = await rows.FirstOrDefaultAsync(x => x.PlayerId == callerId, ct);
                    if (mine is not null)
                    {
                        // Competition ranking: 1 + players strictly ahead on
                        // the sort keys. Exact three-key ties share a rank.
                        var myRank = 1 + await rows
                            .Where(x => x.RankPoints > mine.RankPoints
                                || (x.RankPoints == mine.RankPoints && x.Wins > mine.Wins)
                                || (x.RankPoints == mine.RankPoints && x.Wins == mine.Wins && x.MatchesPlayed < mine.MatchesPlayed))
                            .CountAsync(ct);

                        users.TryGetValue(callerId, out var meUser);
                        me = new PublicSeasonLeaderboardEntryDto(
                            myRank,
                            mine.PlayerId,
                            meUser?.Handle ?? "unknown",
                            meUser?.Handle ?? "Unknown",
                            meUser?.AvatarUrl,
                            mine.RankPoints,
                            mine.Wins,
                            mine.Losses,
                            mine.Draws,
                            mine.Tier,
                            mine.TierRank);
                    }
                }
            }

            return Results.Ok(new PublicSeasonLeaderboardResponseDto(
                season.Id,
                season.SeasonNumber,
                season.Name,
                hasSnapshot,
                page,
                pageSize,
                total,
                totalPages,
                items,
                me));
        }

        /// <summary>
        /// Shared projection for live-profile and snapshot sources. Settable
        /// properties + member-init projection keep the query composable in EF.
        /// </summary>
        private sealed class LeaderboardRow
        {
            public Guid PlayerId { get; set; }
            public int RankPoints { get; set; }
            public int Wins { get; set; }
            public int Losses { get; set; }
            public int Draws { get; set; }
            public int MatchesPlayed { get; set; }
            public int Tier { get; set; }
            public int TierRank { get; set; }
        }
    }
}
