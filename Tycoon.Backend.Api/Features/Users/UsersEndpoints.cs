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

            usersGroup.MapPatch("/me", UpdateCurrentUserProfile);
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
    }
}
