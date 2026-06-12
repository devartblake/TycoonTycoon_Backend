using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.GameEvents;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.GameEvents
{
    public static class GameEventsEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/game-events").WithTags("GameEvents");

            g.MapPost("/enter", async (
                [FromBody] EnterGameEventRequest req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(new EnterGameEvent(req.EventId, req.GameEventId, req.PlayerId), ct);
                return res.Status switch
                {
                    "FeatureDisabled" => ApiResponses.Error(StatusCodes.Status403Forbidden, "FeatureDisabled", "This feature is not available in the current release."),
                    "InvalidStatus" => ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_STATUS", "Game event is not open for entry."),
                    "InsufficientFunds" => ApiResponses.Error(StatusCodes.Status402PaymentRequired, "INSUFFICIENT_FUNDS", "Not enough coins to enter."),
                    "NotFound" => ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Game event not found."),
                    _ => Results.Ok(res)
                };
            }).RequireAuthorization();

            g.MapPost("/revive", async (
                [FromBody] ReviveInGameEventRequest req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(new ReviveInGameEvent(req.EventId, req.GameEventId, req.PlayerId), ct);
                return res.Status switch
                {
                    "NotFound" or "NotParticipant" => ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Participant not found."),
                    "NotAllowed" => ApiResponses.Error(StatusCodes.Status400BadRequest, "NOT_ALLOWED", "Revives are only available in Global Crown events."),
                    "NotEliminated" => ApiResponses.Error(StatusCodes.Status400BadRequest, "NOT_ELIMINATED", "Player is not eliminated."),
                    "InvalidStatus" => ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_STATUS", "Event is not live."),
                    "InsufficientFunds" => ApiResponses.Error(StatusCodes.Status402PaymentRequired, "INSUFFICIENT_FUNDS", "Not enough diamonds."),
                    _ => Results.Ok(res)
                };
            }).RequireAuthorization();

            g.MapGet("/{gameEventId:guid}", async (
                [FromRoute] Guid gameEventId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(new GetGameEventStatus(gameEventId), ct);
                return res is null
                    ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Game event not found.")
                    : Results.Ok(res);
            });

            g.MapGet("/upcoming", async (
                [FromQuery] int? tierId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(new ListUpcomingGameEvents(tierId), ct);
                return Results.Ok(res);
            });
        }
    }
}
