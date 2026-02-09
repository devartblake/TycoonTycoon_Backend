using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Application.Social;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Friends
{
    public static class FriendsEndpoints
    {
        public sealed record SendRequest(Guid FromPlayerId, Guid ToPlayerId);
        public sealed record RespondRequest(Guid PlayerId);

        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/friends")
                .WithTags("Friends").WithOpenApi();

            // POST /friends/request
            g.MapPost("/request", async (
                [FromBody] SendRequest req,
                FriendsService friends,
                CancellationToken ct) =>
            {
                try
                {
                    var dto = await friends.SendRequestAsync(req.FromPlayerId, req.ToPlayerId, ct);
                    return Results.Ok(dto);
                }
                catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
                catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
            });

            // POST /friends/request/{requestId}/accept
            g.MapPost("/request/{requestId:guid}/accept", async (
                Guid requestId,
                [FromBody] RespondRequest req,
                FriendsService friends,
                CancellationToken ct) =>
            {
                try
                {
                    var dto = await friends.AcceptRequestAsync(requestId, req.PlayerId, ct);
                    return dto is null ? Results.NotFound() : Results.Ok(dto);
                }
                catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
                catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
            });

            // POST /friends/request/{requestId}/decline
            g.MapPost("/request/{requestId:guid}/decline", async (
                Guid requestId,
                [FromBody] RespondRequest req,
                FriendsService friends,
                CancellationToken ct) =>
            {
                try
                {
                    var dto = await friends.DeclineRequestAsync(requestId, req.PlayerId, ct);
                    return dto is null ? Results.NotFound() : Results.Ok(dto);
                }
                catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
                catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
            });

            // GET /friends?playerId=...&page=1&pageSize=50
            g.MapGet("", async (
                [FromQuery] Guid playerId,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                FriendsService friends,
                CancellationToken ct) =>
            {
                try
                {
                    var res = await friends.ListFriendsAsync(playerId, page, pageSize, ct);
                    return Results.Ok(res);
                }
                catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
            });

            // GET /friends/requests?playerId=...&box=incoming|outgoing|all&page=1&pageSize=50
            g.MapGet("/requests", async (
                [FromQuery] Guid playerId,
                [FromQuery] string? box,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                FriendsService friends,
                CancellationToken ct) =>
            {
                try
                {
                    var res = await friends.ListRequestsAsync(playerId, box ?? "all", page, pageSize, ct);
                    return Results.Ok(res);
                }
                catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
            });
        }
    }
}
