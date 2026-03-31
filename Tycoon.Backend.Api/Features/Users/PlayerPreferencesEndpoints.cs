using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Users
{
    public static class PlayerPreferencesEndpoints
    {
        private static readonly HashSet<string> ValidModes = new(StringComparer.OrdinalIgnoreCase)
            { "kids", "teen", "adult" };

        private static readonly HashSet<string> ValidSurfaces = new(StringComparer.OrdinalIgnoreCase)
            { "hub", "arena", "labs", "pathways", "journey", "circles", "command" };

        private static readonly HashSet<string> ValidTones = new(StringComparer.OrdinalIgnoreCase)
            { "playful", "balanced", "competitive" };

        public static void Map(WebApplication app)
        {
            var group = app.MapGroup("/users/me/preferences")
                .WithTags("Users")
                .RequireAuthorization();

            group.MapGet("", GetPreferences);
            group.MapPut("", UpdatePreferences);
        }

        private static async Task<IResult> GetPreferences(
            HttpContext httpContext,
            IAppDb database,
            CancellationToken ct)
        {
            if (!TryGetUserId(httpContext, out var userId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var prefs = await database.PlayerPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlayerId == userId, ct);

            if (prefs is null)
            {
                // Return defaults — the player hasn't set preferences yet
                return Results.Ok(new PlayerPreferencesDto("adult", "hub", false, "balanced"));
            }

            return Results.Ok(new PlayerPreferencesDto(
                prefs.SynaptixMode,
                prefs.PreferredSurface,
                prefs.ReducedMotion,
                prefs.TonePreference));
        }

        private static async Task<IResult> UpdatePreferences(
            [FromBody] UpdatePlayerPreferencesRequest request,
            HttpContext httpContext,
            IAppDb database,
            CancellationToken ct)
        {
            if (!TryGetUserId(httpContext, out var userId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            // Validate inputs when provided
            if (request.SynaptixMode is not null && !ValidModes.Contains(request.SynaptixMode))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_MODE",
                    $"SynaptixMode must be one of: {string.Join(", ", ValidModes)}");

            if (request.PreferredSurface is not null && !ValidSurfaces.Contains(request.PreferredSurface))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_SURFACE",
                    $"PreferredSurface must be one of: {string.Join(", ", ValidSurfaces)}");

            if (request.TonePreference is not null && !ValidTones.Contains(request.TonePreference))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_TONE",
                    $"TonePreference must be one of: {string.Join(", ", ValidTones)}");

            var prefs = await database.PlayerPreferences
                .FirstOrDefaultAsync(p => p.PlayerId == userId, ct);

            if (prefs is null)
            {
                prefs = new PlayerPreferences { PlayerId = userId };
                database.PlayerPreferences.Add(prefs);
            }

            if (request.SynaptixMode is not null) prefs.SynaptixMode = request.SynaptixMode;
            if (request.PreferredSurface is not null) prefs.PreferredSurface = request.PreferredSurface;
            if (request.ReducedMotion.HasValue) prefs.ReducedMotion = request.ReducedMotion.Value;
            if (request.TonePreference is not null) prefs.TonePreference = request.TonePreference;
            prefs.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await database.SaveChangesAsync(ct);

            return Results.Ok(new PlayerPreferencesDto(
                prefs.SynaptixMode,
                prefs.PreferredSurface,
                prefs.ReducedMotion,
                prefs.TonePreference));
        }

        private static bool TryGetUserId(HttpContext ctx, out Guid userId)
        {
            userId = Guid.Empty;
            var claim = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim is not null && Guid.TryParse(claim.Value, out userId);
        }
    }
}
