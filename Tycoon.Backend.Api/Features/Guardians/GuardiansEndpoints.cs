using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Guardians;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Guardians
{
    public static class GuardiansEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/guardians").WithTags("Guardians").WithOpenApi();

            g.MapGet("/{tierNumber:int}", async (
                [FromRoute] int tierNumber,
                [FromQuery] Guid seasonId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(new GetGuardiansForTier(seasonId, tierNumber), ct);
                return Results.Ok(res);
            });

            g.MapPost("/challenge", async (
                [FromBody] ChallengeGuardianRequest req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(
                    new ChallengeGuardian(req.EventId, req.SeasonId, req.TierNumber, req.ChallengerId, req.GuardianId), ct);

                return res.Status switch
                {
                    "GuardianNotFound" => ApiResponses.Error(StatusCodes.Status404NotFound, "GUARDIAN_NOT_FOUND", "Guardian not found or expired."),
                    "ChallengeAlreadyPending" => ApiResponses.Error(StatusCodes.Status409Conflict, "CHALLENGE_PENDING", "A challenge is already pending between these players."),
                    _ => Results.Ok(res)
                };
            }).RequireAuthorization();
        }
    }
}
