using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Application.Matches;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Matches
{
    public static class MatchesEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/matches").WithTags("Matches");

            g.MapPost("/start", async ([FromBody] StartMatchRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var res = await mediator.Send(new StartMatch(req.HostPlayerId, req.Mode), ct);
                return Results.Ok(res);
            });
        }
    }
}
