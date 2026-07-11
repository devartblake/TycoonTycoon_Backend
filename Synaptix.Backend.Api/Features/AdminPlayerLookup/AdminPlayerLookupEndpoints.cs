using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Players;

namespace Synaptix.Backend.Api.Features.AdminPlayerLookup;

public static class AdminPlayerLookupEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/player-lookup").WithTags("Admin/Player Lookup");
        g.MapGet("/resolve", Resolve);
        g.MapGet("/search", Search);
    }

    private static async Task<IResult> Search(
        [FromQuery] string query,
        [FromQuery] int limit,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new AdminSearchPlayers(query, limit), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> Resolve(
        [FromQuery] string query,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new AdminResolvePlayerLookup(query), ct);
        return result is null
            ? AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Player lookup did not match a known user, player, or short code.")
            : Results.Ok(result);
    }
}
