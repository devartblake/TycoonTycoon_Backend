using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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

            // No-loss prediction: "will the champion defend?" Open to everyone
            // while the event is Open; correct predictors share a fixed pool.
            g.MapPost("/{gameEventId:guid}/predict", async (
                [FromRoute] Guid gameEventId,
                [FromBody] SubmitPredictionRequest req,
                HttpContext httpContext,
                ChampionPredictionService predictions,
                CancellationToken ct) =>
            {
                if (!TryGetPlayer(httpContext, out var playerId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                var status = await predictions.PredictAsync(gameEventId, playerId, req.ChampionDefends, ct);
                return status switch
                {
                    "Accepted" => Results.Ok(new { status }),
                    "Closed" => ApiResponses.Error(StatusCodes.Status409Conflict, "PREDICTIONS_CLOSED", "Predictions are closed for this match."),
                    "Disabled" => ApiResponses.Error(StatusCodes.Status403Forbidden, "DISABLED", "Predictions are not available."),
                    _ => ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Not a champion event."),
                };
            }).RequireAuthorization();

            // The caller's prediction state + live tally + result.
            g.MapGet("/{gameEventId:guid}/prediction", async (
                [FromRoute] Guid gameEventId,
                HttpContext httpContext,
                ChampionPredictionService predictions,
                CancellationToken ct) =>
            {
                TryGetPlayer(httpContext, out var playerId); // anonymous allowed → empty guid, no personal pick
                var state = await predictions.GetStateAsync(gameEventId, playerId, ct);
                return state is null
                    ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Not a champion event.")
                    : Results.Ok(state);
            });

            // Replay-on-join: the current open round/duel so a client entering
            // mid-match renders live state without waiting for the next push.
            g.MapGet("/{gameEventId:guid}/live", async (
                [FromRoute] Guid gameEventId,
                ChampionMatchOrchestrator orchestrator,
                CancellationToken ct) =>
            {
                var snapshot = await orchestrator.GetLiveSnapshotAsync(gameEventId, ct);
                return snapshot is null
                    ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Not a champion event.")
                    : Results.Ok(snapshot);
            });

            // Live roster: participants with handles + champion/eliminated
            // flags, so the champion can pick a duel target and spectators see
            // the mob shrink.
            g.MapGet("/{gameEventId:guid}/participants", async (
                [FromRoute] Guid gameEventId,
                ChampionMatchOrchestrator orchestrator,
                CancellationToken ct) =>
            {
                var roster = await orchestrator.GetRosterAsync(gameEventId, ct);
                return roster is null
                    ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Not a champion event.")
                    : Results.Ok(roster);
            });

            // Submit an answer to the current live round of a Champion vs Tier
            // match. The player id comes from the JWT (never the body).
            g.MapPost("/{gameEventId:guid}/rounds/answer", async (
                [FromRoute] Guid gameEventId,
                [FromBody] SubmitRoundAnswerRequest req,
                HttpContext httpContext,
                ChampionMatchOrchestrator orchestrator,
                CancellationToken ct) =>
            {
                var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                            ?? httpContext.User.FindFirst("sub");
                if (claim is null || !Guid.TryParse(claim.Value, out var playerId) || playerId == Guid.Empty)
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                var status = await orchestrator.SubmitAnswerAsync(gameEventId, playerId, req.OptionId, ct);
                return status switch
                {
                    "Accepted" => Results.Ok(new { status }),
                    "NoOpenRound" or "RoundClosed" => ApiResponses.Error(StatusCodes.Status409Conflict, "ROUND_CLOSED", "No open round to answer."),
                    "NotParticipant" => ApiResponses.Error(StatusCodes.Status403Forbidden, "NOT_PARTICIPANT", "You are not in this event."),
                    "Eliminated" => ApiResponses.Error(StatusCodes.Status409Conflict, "ELIMINATED", "You have been eliminated."),
                    _ => ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_ANSWER", "Invalid answer."),
                };
            }).RequireAuthorization();
            // Champion calls out a challenger for a head-to-head duel. Only the
            // seeded champion (from the JWT) may initiate.
            g.MapPost("/{gameEventId:guid}/duel", async (
                [FromRoute] Guid gameEventId,
                [FromBody] StartDuelRequest req,
                HttpContext httpContext,
                ChampionMatchOrchestrator orchestrator,
                CancellationToken ct) =>
            {
                if (!TryGetPlayer(httpContext, out var championId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                var status = await orchestrator.StartDuelAsync(gameEventId, championId, req.ChallengerPlayerId, ct);
                return status switch
                {
                    "Started" => Results.Ok(new { status }),
                    "NotChampion" => ApiResponses.Error(StatusCodes.Status403Forbidden, "NOT_CHAMPION", "Only the champion can start a duel."),
                    "NotFound" => ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Event not found."),
                    "InvalidStatus" => ApiResponses.Error(StatusCodes.Status409Conflict, "INVALID_STATUS", "The match is not live."),
                    "ChampionEliminated" => ApiResponses.Error(StatusCodes.Status409Conflict, "ELIMINATED", "You are no longer the champion."),
                    "DuelInProgress" => ApiResponses.Error(StatusCodes.Status409Conflict, "DUEL_IN_PROGRESS", "A duel is already underway."),
                    "DuelLimitReached" => ApiResponses.Error(StatusCodes.Status409Conflict, "DUEL_LIMIT", "No duels remaining this match."),
                    "InvalidChallenger" => ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_CHALLENGER", "Pick an active challenger."),
                    _ => ApiResponses.Error(StatusCodes.Status409Conflict, "UNAVAILABLE", "Duel unavailable right now."),
                };
            }).RequireAuthorization();

            // Either duelist submits their answer to the current duel.
            g.MapPost("/{gameEventId:guid}/duel/answer", async (
                [FromRoute] Guid gameEventId,
                [FromBody] SubmitRoundAnswerRequest req,
                HttpContext httpContext,
                ChampionMatchOrchestrator orchestrator,
                CancellationToken ct) =>
            {
                if (!TryGetPlayer(httpContext, out var playerId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                var status = await orchestrator.SubmitDuelAnswerAsync(gameEventId, playerId, req.OptionId, ct);
                return status switch
                {
                    "Accepted" => Results.Ok(new { status }),
                    "NoOpenDuel" or "DuelClosed" => ApiResponses.Error(StatusCodes.Status409Conflict, "DUEL_CLOSED", "No open duel to answer."),
                    "NotDuelist" => ApiResponses.Error(StatusCodes.Status403Forbidden, "NOT_DUELIST", "You are not in this duel."),
                    _ => ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_ANSWER", "Invalid answer."),
                };
            }).RequireAuthorization();
        }

        private static bool TryGetPlayer(HttpContext httpContext, out Guid playerId)
        {
            playerId = Guid.Empty;
            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                        ?? httpContext.User.FindFirst("sub");
            return claim is not null && Guid.TryParse(claim.Value, out playerId) && playerId != Guid.Empty;
        }

        public sealed record SubmitRoundAnswerRequest(string OptionId);
        public sealed record StartDuelRequest(Guid ChallengerPlayerId);
    }
}
