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
