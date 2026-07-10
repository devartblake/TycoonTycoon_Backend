using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Seasons;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.AdminSeasons
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
                return activated is null
                    ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Season not found.")
                    : Results.Ok(activated);
            });

            g.MapPost("/close", async ([FromBody] CloseSeasonRequest req, SeasonService svc, CancellationToken ct) =>
            {
                var (closed, next) = await svc.CloseAsync(req, ct);
                if (closed is null)
                    return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Season not found.");
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

            // Moderation-only per-player reset: zero the profile's rank points
            // via a negative ledger transaction so the audit trail stays
            // truthful. Full-season resets remain POST /close with
            // carryoverPercent 0.
            g.MapPost("/{seasonId:guid}/players/{playerId:guid}/reset", async (
                [FromRoute] Guid seasonId,
                [FromRoute] Guid playerId,
                [FromBody] ResetPlayerSeasonPointsRequest? req,
                IAppDb db,
                SeasonPointsService points,
                CancellationToken ct) =>
            {
                var profile = await db.PlayerSeasonProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SeasonId == seasonId && x.PlayerId == playerId, ct);
                if (profile is null)
                    return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Player has no profile in this season.");

                if (profile.RankPoints <= 0)
                    return Results.Ok(new { status = "NoOp", rankPoints = profile.RankPoints });

                var res = await points.ApplyAsync(new ApplySeasonPointsRequest(
                    Guid.NewGuid(),
                    seasonId,
                    playerId,
                    "moderation-reset",
                    -profile.RankPoints,
                    string.IsNullOrWhiteSpace(req?.Reason) ? "moderation reset" : req!.Reason!.Trim()), ct);

                return Results.Ok(new { status = res.Status, rankPoints = res.NewRankPoints });
            });

            // Manual tiebreaker scheduling (ops/support/special events).
            g.MapPost("/{seasonId:guid}/tiebreakers", async (
                [FromRoute] Guid seasonId,
                [FromBody] CreateSeasonTiebreakerRequest req,
                IAppDb db,
                IOptions<SeasonTiebreakerOptions> options,
                CancellationToken ct) =>
            {
                var players = req.PlayerIds?.Distinct().ToList() ?? [];
                if (players.Count < 2)
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "At least two distinct playerIds are required.");

                var seasonExists = await db.Seasons.AsNoTracking().AnyAsync(x => x.Id == seasonId, ct);
                if (!seasonExists)
                    return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Season not found.");

                var opts = options.Value;
                var scheduledAt = req.ScheduledAtUtc ?? DateTimeOffset.UtcNow + opts.ScheduleDelay;
                var tiebreaker = new SeasonTiebreaker(
                    seasonId,
                    string.IsNullOrWhiteSpace(req.Scope) ? SeasonTiebreaker.Scopes.Custom : req.Scope!,
                    tier: 0,
                    boundaryRank: 0,
                    rankPoints: 0,
                    players,
                    scheduledAt,
                    scheduledAt + opts.ExpiryGrace);

                db.SeasonTiebreakers.Add(tiebreaker);
                await db.SaveChangesAsync(ct);
                return Results.Ok(new { tiebreakerId = tiebreaker.Id, tiebreaker.Status, tiebreaker.ScheduledAtUtc, tiebreaker.ExpiresAtUtc });
            });

            // Cancel a pending tiebreaker; standings finalize deterministically.
            g.MapPost("/tiebreakers/{tiebreakerId:guid}/cancel", async (
                [FromRoute] Guid tiebreakerId,
                [FromBody] CancelSeasonTiebreakerRequest? req,
                SeasonTiebreakerService tiebreakers,
                CancellationToken ct) =>
            {
                var cancelled = await tiebreakers.CancelAsync(tiebreakerId, req?.Note, ct);
                return cancelled is null
                    ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "No pending tiebreaker with that id.")
                    : Results.Ok(new { cancelled.Id, cancelled.Status, cancelled.WinnerPlayerId });
            });

            // Manual resolution lever for support cases.
            g.MapPost("/tiebreakers/{tiebreakerId:guid}/resolve", async (
                [FromRoute] Guid tiebreakerId,
                [FromBody] ResolveSeasonTiebreakerRequest req,
                SeasonTiebreakerService tiebreakers,
                CancellationToken ct) =>
            {
                if (req.WinnerPlayerId == Guid.Empty)
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "winnerPlayerId is required.");

                var resolved = await tiebreakers.ResolveAsync(
                    tiebreakerId, req.WinnerPlayerId, matchId: null,
                    note: string.IsNullOrWhiteSpace(req.Note) ? "resolved by operator" : req.Note!.Trim(), ct);
                return resolved is null
                    ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "No pending tiebreaker with that id, or the winner is not a participant.")
                    : Results.Ok(new { resolved.Id, resolved.Status, resolved.WinnerPlayerId });
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
