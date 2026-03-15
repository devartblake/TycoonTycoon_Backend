using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Votes;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Votes
{
    public static class VoteEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/votes").WithTags("Votes").WithOpenApi();

            // POST /votes
            g.MapPost("/", async (
                [FromBody] CastVoteRequest req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(new CastVote(req.PlayerId, req.Option, req.Topic), ct);

                return result.Status switch
                {
                    CastVoteStatus.Recorded     => Results.Ok(result.Vote),
                    CastVoteStatus.DuplicateVote => ApiResponses.Error(
                        StatusCodes.Status409Conflict, "DUPLICATE_VOTE",
                        "Player has already voted on this topic."),
                    CastVoteStatus.InvalidOption => ApiResponses.Error(
                        StatusCodes.Status400BadRequest, "INVALID_OPTION",
                        $"Option must be one of: {string.Join(", ", VoteOptions.Valid)}."),
                    _ => ApiResponses.Error(StatusCodes.Status500InternalServerError, "UNKNOWN", "Unexpected error.")
                };
            }).RequireAuthorization();

            // GET /votes/{topic}/results
            g.MapGet("/{topic}/results", async (
                [FromRoute] string topic,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var results = await mediator.Send(new GetVoteResults(topic), ct);
                return Results.Ok(results);
            });
        }
    }
}
