using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Quiz;

namespace Synaptix.Backend.Api.Features.Quiz;

public static class QuizEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/quiz").WithTags("Quiz").RequireAuthorization();

        g.MapPost("/complete", async (
            [FromBody] CompleteQuizRequest req,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (req.PlayerId == Guid.Empty || req.EventId == Guid.Empty)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId and eventId are required.");

            var res = await mediator.Send(
                new CompleteQuiz(req.PlayerId, req.EventId, req.XpEarned, req.CoinsEarned), ct);

            return Results.Ok(res);
        }).RequireRateLimiting("matches-submit");
    }

    public sealed record CompleteQuizRequest(
        Guid PlayerId,
        Guid EventId,
        int XpEarned,
        int CoinsEarned
    );
}
