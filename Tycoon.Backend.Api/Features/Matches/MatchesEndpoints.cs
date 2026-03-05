using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Enforcement;
using Tycoon.Backend.Application.Matches;
using Tycoon.Backend.Application.Moderation;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Matches
{
    public static class MatchesEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/matches").WithTags("Matches").WithOpenApi();

            g.MapPost("/start", async (
                [FromBody] StartMatchRequest req,
                EnforcementService enforcement,
                ModerationService moderation,
                IMediator mediator,
                CancellationToken ct) =>
            {
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

            g.MapGet("/{matchId:guid}", async ([FromRoute] Guid matchId, IAppDb db, CancellationToken ct) =>
            {
                // Query: match + result + participants (grid-friendly and stable for UI)
                var match = await db.Matches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == matchId, ct);
                if (match is null) return Results.NotFound();

                var result = await db.MatchResults.AsNoTracking().FirstOrDefaultAsync(x => x.MatchId == matchId, ct);
                if (result is null) return Results.NotFound();

                var parts = await db.MatchParticipantResults.AsNoTracking()
                    .Where(x => x.MatchResultId == result.Id)
                    .Select(x => new MatchParticipantResultDto(x.PlayerId, x.Score, x.Correct, x.Wrong, x.AvgAnswerTimeMs))
                    .ToListAsync(ct);

                return Results.Ok(new MatchDetailDto(
                    MatchId: match.Id,
                    HostPlayerId: match.HostPlayerId,
                    Mode: result.Mode,
                    Category: result.Category,
                    QuestionCount: result.QuestionCount,
                    StartedAtUtc: match.StartedAt,
                    EndedAtUtc: result.EndedAtUtc,
                    Status: result.Status,
                    Participants: parts
                ));
            });

        }
    }
}
