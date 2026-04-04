using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Users
{
    public static class UsersEndpoints
    {
        public static void Map(WebApplication app)
        {
            var usersGroup = app.MapGroup("/users")
                .WithTags("Users")
                .RequireAuthorization();

            usersGroup.MapGet("/search", SearchUsers);
            usersGroup.MapGet("/{userId:guid}/career-summary", GetCareerSummary);
            usersGroup.MapPatch("/me", UpdateCurrentUserProfile);
        }

        private static async Task<IResult> SearchUsers(
            [FromQuery] string handle,
            IAppDb database,
            CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(handle) || handle.Trim().Length < 2)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Query parameter 'handle' must be at least 2 characters.");

            var normalizedHandle = handle.Trim().ToLowerInvariant();

            var users = await database.Users
                .Where(u => u.Handle.ToLower().Contains(normalizedHandle))
                .OrderBy(u => u.Handle)
                .Take(20)
                .Select(u => new UserDto(
                    u.Id,
                    u.Handle,
                    u.Email,
                    u.Country,
                    u.Tier,
                    u.Mmr
                ))
                .ToListAsync(cancellation);

            return Results.Ok(users);
        }

        private static async Task<IResult> UpdateCurrentUserProfile(
            [FromBody] UpdateProfileRequest request,
            HttpContext httpContext,
            IAppDb database,
            CancellationToken cancellation)
        {
            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim?.Value))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            if (!Guid.TryParse(userIdClaim.Value, out var parsedUserId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Invalid authenticated user identifier.");

            var currentUser = await database.Users
                .FirstOrDefaultAsync(u => u.Id == parsedUserId, cancellation);

            if (currentUser is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "User not found.");

            currentUser.UpdateProfile(request.Handle, request.Country);
            await database.SaveChangesAsync(cancellation);

            var updatedProfile = new UserDto(
                currentUser.Id,
                currentUser.Handle,
                currentUser.Email,
                currentUser.Country,
                currentUser.Tier,
                currentUser.Mmr
            );

            return Results.Ok(updatedProfile);
        }

        private static async Task<IResult> GetCareerSummary(
            Guid userId,
            IAppDb database,
            CancellationToken cancellation)
        {
            if (userId == Guid.Empty)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "userId cannot be empty.");

            var exists = await database.Users
                .AnyAsync(u => u.Id == userId, cancellation);

            if (!exists)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "User not found.");

            var aggregate = await database.PlayerSeasonProfiles
                .Where(x => x.PlayerId == userId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Wins = g.Sum(x => x.Wins),
                    Losses = g.Sum(x => x.Losses),
                    Draws = g.Sum(x => x.Draws),
                    MatchesPlayed = g.Sum(x => x.MatchesPlayed)
                })
                .FirstOrDefaultAsync(cancellation);

            var wins = aggregate?.Wins ?? 0;
            var losses = aggregate?.Losses ?? 0;
            var draws = aggregate?.Draws ?? 0;
            var matchesPlayed = aggregate?.MatchesPlayed ?? 0;
            var winRate = matchesPlayed > 0
                ? Math.Round((decimal)wins / matchesPlayed, 4, MidpointRounding.AwayFromZero)
                : 0m;

            return Results.Ok(new UserCareerSummaryDto(
                UserId: userId,
                Wins: wins,
                Losses: losses,
                Draws: draws,
                MatchesPlayed: matchesPlayed,
                WinRate: winRate));
        }
    }
}
