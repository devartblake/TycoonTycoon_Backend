using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Application.Players;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Players
{
    public static class PlayersEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/players").WithTags("Players").WithOpenApi();

            g.MapPost("/", async ([FromBody] CreatePlayerRequest req, AppDb db, CancellationToken ct) =>
            {
                var p = new Player(req.Username, string.IsNullOrWhiteSpace(req.CountryCode) ? "US" : req.CountryCode);
                db.Players.Add(p);
                await db.SaveChangesAsync(ct);

                return Results.Created($"/players/{p.Id}",
                    new PlayerDto(p.Id, p.Username, p.CountryCode, p.Level, p.Xp));
            });

            g.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new GetPlayerById(id), ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            });
        }
    }
}
