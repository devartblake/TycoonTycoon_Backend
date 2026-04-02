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

            usersGroup.MapGet("/search", SearchUsers).WithOpenApi();
            usersGroup.MapPatch("/me", UpdateCurrentUserProfile);
        }

        private static async Task<IResult> SearchUsers(
            [FromQuery] string handle,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            IAppDb database = default!,
            CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(handle) || handle.Trim().Length < 2)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_QUERY",
                    "Handle search requires at least 2 characters.");

            page = page <= 0 ? 1 : page;
            pageSize = pageSize is <= 0 or > 50 ? 20 : pageSize;

            var pattern = $"%{handle.Trim()}%";

            var query = database.Users.AsNoTracking()
                .Where(u => u.IsActive && EF.Functions.ILike(u.Handle, pattern))
                .OrderBy(u => u.Handle);

            var total = await query.CountAsync(cancellation);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserSearchResultDto(u.Id, u.Handle, u.Country, u.Tier, u.Mmr))
                .ToListAsync(cancellation);

            return Results.Ok(new UserSearchResponseDto(page, pageSize, total, items));
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
