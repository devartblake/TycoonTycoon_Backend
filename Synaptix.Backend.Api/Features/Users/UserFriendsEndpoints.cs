using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Social;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Users
{
    public static class UserFriendsEndpoints
    {
        public sealed record SendFriendRequestBody(Guid TargetUserId);

        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/users/me/friends")
                .WithTags("Friends")
                
                .RequireAuthorization();

            // GET /users/me/friends
            g.MapGet("", async (
                HttpContext httpContext,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                FriendsService friends,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(httpContext, out var playerId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                try
                {
                    var res = await friends.ListFriendsAsync(playerId, page, pageSize, ct);
                    return Results.Ok(res);
                }
                catch (ArgumentException ex) { return ApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", ex.Message); }
            });

            // POST /users/me/friends/request
            g.MapPost("/request", async (
                HttpContext httpContext,
                [FromBody] SendFriendRequestBody req,
                FriendsService friends,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(httpContext, out var playerId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                try
                {
                    var dto = await friends.SendRequestAsync(playerId, req.TargetUserId, ct);
                    return Results.Ok(dto);
                }
                catch (ArgumentException ex) { return ApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", ex.Message); }
                catch (InvalidOperationException ex) { return ApiResponses.Error(StatusCodes.Status409Conflict, "CONFLICT", ex.Message); }
            });

            // GET /users/me/friends/requests  (incoming pending)
            g.MapGet("/requests", async (
                HttpContext httpContext,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                FriendsService friends,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(httpContext, out var playerId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                try
                {
                    var res = await friends.ListRequestsDetailAsync(playerId, "incoming", page, pageSize, ct);
                    return Results.Ok(res);
                }
                catch (ArgumentException ex) { return ApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", ex.Message); }
            });

            // GET /users/me/friends/requests/sent  (outgoing)
            g.MapGet("/requests/sent", async (
                HttpContext httpContext,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                FriendsService friends,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(httpContext, out var playerId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                try
                {
                    var res = await friends.ListRequestsDetailAsync(playerId, "outgoing", page, pageSize, ct);
                    return Results.Ok(res);
                }
                catch (ArgumentException ex) { return ApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", ex.Message); }
            });

            // POST /users/me/friends/requests/{requestId}/accept
            g.MapPost("/requests/{requestId:guid}/accept", async (
                Guid requestId,
                HttpContext httpContext,
                FriendsService friends,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(httpContext, out var playerId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                try
                {
                    var dto = await friends.AcceptRequestAsync(requestId, playerId, ct);
                    return dto is null
                        ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Friend request not found.")
                        : Results.Ok(dto);
                }
                catch (ArgumentException ex) { return ApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", ex.Message); }
                catch (InvalidOperationException ex) { return ApiResponses.Error(StatusCodes.Status409Conflict, "CONFLICT", ex.Message); }
            });

            // POST /users/me/friends/requests/{requestId}/decline
            g.MapPost("/requests/{requestId:guid}/decline", async (
                Guid requestId,
                HttpContext httpContext,
                FriendsService friends,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(httpContext, out var playerId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                try
                {
                    var dto = await friends.DeclineRequestAsync(requestId, playerId, ct);
                    return dto is null
                        ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Friend request not found.")
                        : Results.Ok(dto);
                }
                catch (ArgumentException ex) { return ApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", ex.Message); }
                catch (InvalidOperationException ex) { return ApiResponses.Error(StatusCodes.Status409Conflict, "CONFLICT", ex.Message); }
            });

            // GET /users/me/friends/suggestions
            g.MapGet("/suggestions", async (
                HttpContext httpContext,
                IAppDb db,
                FriendsService friends,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(httpContext, out var playerId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

                // Collect existing friend IDs to exclude
                var existingFriendIds = await db.FriendEdges.AsNoTracking()
                    .Where(e => e.PlayerId == playerId)
                    .Select(e => e.FriendPlayerId)
                    .ToListAsync(ct);

                var excluded = new HashSet<Guid>(existingFriendIds) { playerId };

                var suggestions = await db.Users.AsNoTracking()
                    .Where(u => !excluded.Contains(u.Id) && u.IsActive)
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .Select(u => new FriendSuggestionDto(
                        u.Id,
                        u.Handle,
                        u.Handle,
                        u.AvatarUrl,
                        0,
                        "New to Synaptix"
                    ))
                    .ToListAsync(ct);

                return Results.Ok(suggestions);
            });
        }

        private static bool TryGetUserId(HttpContext httpContext, out Guid userId)
        {
            userId = Guid.Empty;
            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                        ?? httpContext.User.FindFirst("sub");
            return claim is not null && Guid.TryParse(claim.Value, out userId) && userId != Guid.Empty;
        }
    }
}
