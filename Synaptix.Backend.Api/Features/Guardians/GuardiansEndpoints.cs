using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Guardians;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Guardians
{
    public static class GuardiansEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/guardians").WithTags("Guardians");

            g.MapGet("/{tierNumber:int}", async (
                [FromRoute] int tierNumber,
                [FromQuery] Guid seasonId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(new GetGuardiansForTier(seasonId, tierNumber), ct);
                return Results.Ok(res);
            });

            // Is this player currently a tier guardian? (optionally scoped to a season)
            g.MapGet("/my/{playerId:guid}", async (
                [FromRoute] Guid playerId,
                [FromQuery] Guid? seasonId,
                IAppDb db,
                CancellationToken ct) =>
            {
                var now = DateTimeOffset.UtcNow;
                var q = db.TierGuardians.AsNoTracking()
                    .Where(x => x.PlayerId == playerId && x.ExpiresAtUtc > now);
                if (seasonId is Guid sid)
                    q = q.Where(x => x.SeasonId == sid);

                var guardian = await q
                    .OrderByDescending(x => x.AssignedAtUtc)
                    .FirstOrDefaultAsync(ct);

                if (guardian is null)
                    return Results.Ok(new MyGuardianStatusDto(playerId, false, null, 0, null));

                // GuardianChallenge.GuardianId stores the guardian *player's* id.
                var currentMatchId = await db.GuardianChallenges.AsNoTracking()
                    .Where(x => x.SeasonId == guardian.SeasonId
                             && x.TierNumber == guardian.TierNumber
                             && x.GuardianId == playerId
                             && x.Status == ChallengeStatus.Pending)
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .Select(x => (Guid?)x.MatchId)
                    .FirstOrDefaultAsync(ct);

                return Results.Ok(new MyGuardianStatusDto(
                    playerId, true, guardian.TierNumber, guardian.DefencesWon, currentMatchId));
            }).RequireAuthorization();

            g.MapPost("/challenge", async (
                [FromBody] ChallengeGuardianRequest req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(
                    new ChallengeGuardian(req.EventId, req.SeasonId, req.TierNumber, req.ChallengerId, req.GuardianId), ct);

                return res.Status switch
                {
                    "FeatureDisabled" => ApiResponses.Error(StatusCodes.Status403Forbidden, "FeatureDisabled", "This feature is not available in the current release."),
                    "GuardianNotFound" => ApiResponses.Error(StatusCodes.Status404NotFound, "GUARDIAN_NOT_FOUND", "Guardian not found or expired."),
                    "ChallengeAlreadyPending" => ApiResponses.Error(StatusCodes.Status409Conflict, "CHALLENGE_PENDING", "A challenge is already pending between these players."),
                    _ => Results.Ok(res)
                };
            }).RequireAuthorization();
        }
    }
}
