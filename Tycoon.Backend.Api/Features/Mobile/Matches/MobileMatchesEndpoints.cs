using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Enforcement;
using Tycoon.Backend.Application.Matches;
using Tycoon.Backend.Application.Moderation;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Mobile.Matches
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
                .WithOpenApi();

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

                var res = await mediator.Send(new StartMatch(req.HostPlayerId, req.Mode), ct);
                return Results.Ok(res);
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
