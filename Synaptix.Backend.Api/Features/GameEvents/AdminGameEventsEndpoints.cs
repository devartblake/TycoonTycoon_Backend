using System.Linq;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.GameEvents;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.GameEvents
{
    public static class AdminGameEventsEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/game-events").WithTags("Admin/GameEvents");

            // Comp a premium spectator pass (support/marketing). Days omitted =
            // permanent; otherwise a seasonal pass expiring in N days.
            g.MapPost("/spectator-pass", async (
                [FromBody] GrantSpectatorPassRequest req,
                ChampionSpectatorService spectator,
                CancellationToken ct) =>
            {
                if (req.PlayerId == Guid.Empty)
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId is required.");
                await spectator.GrantPassAsync(req.PlayerId, req.Days, ct);
                return Results.Ok(new { status = "Granted", req.PlayerId, req.Days });
            });

            // Attribute an event's jackpot boost to a sponsor. Blank name clears
            // the attribution (house-funded). No-op once the event has closed.
            g.MapPost("/{gameEventId:guid}/sponsor", async (
                [FromRoute] Guid gameEventId,
                [FromBody] SetEventSponsorRequest req,
                IAppDb db,
                CancellationToken ct) =>
            {
                var ev = await db.GameEvents.FirstOrDefaultAsync(x => x.Id == gameEventId, ct);
                if (ev is null)
                    return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Game event not found.");
                if (ev.Status == GameEventStatus.Closed)
                    return AdminApiResponses.Error(StatusCodes.Status409Conflict, "EVENT_CLOSED", "Cannot change the sponsor of a closed event.");

                ev.ApplySponsor(req.SponsorName, req.Multiplier);
                await db.SaveChangesAsync(ct);

                return Results.Ok(new EventSponsorDto(
                    ev.Id, ev.SponsorName, ev.JackpotMultiplier,
                    ev.JackpotPool, ev.EffectiveJackpot, ev.SponsorBoostAmount));
            });

            // Reconciliation report: closed-event sponsor boosts, optionally
            // filtered by sponsor/season, with per-sponsor totals.
            g.MapGet("/sponsor-attributions", async (
                [FromQuery] string? sponsor,
                [FromQuery] Guid? seasonId,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                IAppDb db,
                CancellationToken ct) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize == 0 ? 20 : pageSize, 1, 100);

                var query = db.GameEventSponsorAttributions.AsNoTracking();
                if (!string.IsNullOrWhiteSpace(sponsor))
                    query = query.Where(x => x.SponsorName == sponsor);
                if (seasonId is Guid sid)
                    query = query.Where(x => x.SeasonId == sid);

                var total = await query.CountAsync(ct);
                var items = await query
                    .OrderByDescending(x => x.RecordedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new SponsorAttributionDto(
                        x.GameEventId, x.SponsorName, x.BaseJackpot, x.Multiplier,
                        x.EffectiveJackpot, x.BoostAmount, x.BeneficiaryPlayerId,
                        x.SeasonId, x.RecordedAtUtc))
                    .ToListAsync(ct);

                var summary = await query
                    .GroupBy(x => x.SponsorName)
                    .Select(gr => new SponsorAttributionSummaryDto(
                        gr.Key, gr.Count(), gr.Sum(x => x.BoostAmount)))
                    .OrderByDescending(s => s.TotalBoostFunded)
                    .ToListAsync(ct);

                return Results.Ok(new { page, pageSize, total, items, summary });
            });

            g.MapGet("/", async (
                [FromQuery] int page,
                [FromQuery] int pageSize,
                [FromQuery] string? status,
                IAppDb db,
                CancellationToken ct) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize == 0 ? 20 : pageSize, 1, 100);

                var query = db.GameEvents.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(status) &&
                    Enum.TryParse<GameEventStatus>(status, ignoreCase: true, out var statusEnum))
                {
                    query = query.Where(e => e.Status == statusEnum);
                }

                var total = await query.CountAsync(ct);
                var items = await query
                    .OrderByDescending(e => e.ScheduledAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new GameEventSummaryDto(e.Id, e.Kind, e.TierId, e.Status, e.ScheduledAtUtc, e.EntryFeeCoins, e.MaxParticipants))
                    .ToListAsync(ct);

                return Results.Ok(new { page, pageSize, total, items });
            });

            g.MapPost("/", async (
                [FromBody] CreateGameEventRequest req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(new AdminCreateGameEvent(req), ct);
                return Results.Ok(res);
            });

            g.MapPost("/{gameEventId:guid}/open", async (
                [FromRoute] Guid gameEventId,
                IAppDb db,
                CancellationToken ct) =>
            {
                var ev = await db.GameEvents.FirstOrDefaultAsync(x => x.Id == gameEventId, ct);
                if (ev is null)
                    return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Game event not found.");

                ev.Open(DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(ct);
                return Results.Ok(new { status = ev.Status.ToString() });
            });

            g.MapPost("/{gameEventId:guid}/start", async (
                [FromRoute] Guid gameEventId,
                IAppDb db,
                CancellationToken ct) =>
            {
                var ev = await db.GameEvents.FirstOrDefaultAsync(x => x.Id == gameEventId, ct);
                if (ev is null)
                    return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Game event not found.");

                ev.Start(DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(ct);
                return Results.Ok(new { status = ev.Status.ToString() });
            });

            g.MapPost("/{gameEventId:guid}/close", async (
                [FromRoute] Guid gameEventId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(new CloseGameEventAndDistributePrizes(gameEventId), ct);
                return Results.Ok(res);
            });
        }
    }
}
