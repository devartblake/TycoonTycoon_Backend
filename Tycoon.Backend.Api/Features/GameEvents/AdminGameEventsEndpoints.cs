using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.GameEvents;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.GameEvents
{
    public static class AdminGameEventsEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/game-events").WithTags("Admin/GameEvents").WithOpenApi();

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
