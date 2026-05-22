using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Enforcement;
using Synaptix.Backend.Application.Matches;
using Synaptix.Backend.Application.Moderation;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Mobile.Matches
{
    /// <summary>
    /// Sample mobile endpoints layout.
    ///
    /// NOTE:
    /// - Keep business logic in application handlers/services.
    /// - Mobile endpoints may later diverge in DTO shape, defaults, and rate limits.
    /// </summary>
    public static class MobileMatchesEndpoints
    {
        public static void Map(RouteGroupBuilder mobile)
        {
            var g = mobile.MapGroup("/matches")
                .WithTags("Mobile/Matches")
                ;

            g.MapPost("/start", async (
                [FromBody] StartMatchRequest req,
                EnforcementService enforcement,
                ModerationService moderation,
                IMediator mediator,
                CancellationToken ct) =>
            {
                // Mirror existing guard rails; keep behavior parity with regular endpoint by default.
                var decision = await enforcement.EvaluateAsync(req.HostPlayerId, ct);
                if (!decision.CanStartMatch)
                    return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Player is not allowed to start matches.");

                var status = await moderation.GetEffectiveStatusAsync(req.HostPlayerId, ct);
                if (status == ModerationStatus.Banned)
                    return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Player is not allowed to start matches.");

                try
                {
                    var res = await mediator.Send(new StartMatch(req.HostPlayerId, req.Mode), ct);
                    return Results.Ok(res);
                }
                catch (ModeEntryDeniedException ex)
                {
                    return ApiResponses.Error(
                        StatusCodes.Status409Conflict,
                        "MATCH_ENTRY_DENIED",
                        ex.Message,
                        new { reasonCode = ex.ReasonCode, mode = req.Mode });
                }
                catch (InvalidOperationException ex)
                {
                    return ApiResponses.Error(StatusCodes.Status409Conflict, "CONFLICT", ex.Message);
                }
            });

            g.MapPost("/submit", async (
                [FromBody] SubmitMatchRequest req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(new SubmitMatch(req), ct);
                return Results.Ok(res);
            }).RequireRateLimiting("matches-submit");
        }
    }
}
