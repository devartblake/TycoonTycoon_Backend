using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
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
            group.MapGet("/loadout", GetLoadout);
            group.MapPut("/loadout", UpdateLoadout);
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
            // JWT middleware runs with MapInboundClaims=false, so "sub" is not remapped to
            // ClaimTypes.NameIdentifier. Try "sub" first, fall back for other auth schemes.
            var claim = ctx.User.FindFirst("sub")
                     ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim is not null && Guid.TryParse(claim.Value, out userId);
        }

        private static async Task<IResult> GetLoadout(
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
                return Results.Ok(new PlayerLoadoutDto(null, Array.Empty<string>()));

            return Results.Ok(new PlayerLoadoutDto(
                prefs.AvatarItemType,
                ParseCsv(prefs.EquippedCosmeticItemTypesCsv)));
        }

        private static async Task<IResult> UpdateLoadout(
            [FromBody] UpdatePlayerLoadoutRequest request,
            HttpContext httpContext,
            IAppDb database,
            CancellationToken ct)
        {
            if (!TryGetUserId(httpContext, out var userId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var desiredAvatar = string.IsNullOrWhiteSpace(request.AvatarItemType)
                ? null
                : request.AvatarItemType.Trim().ToLowerInvariant();

            if (desiredAvatar is not null && !IsLoadoutItemType(desiredAvatar))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_LOADOUT_ITEM", "Avatar item type must start with 'avatar:' or 'cosmetic:'.");

            var desiredCosmetics = (request.EquippedCosmeticItemTypes ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            if (desiredCosmetics.Any(x => !x.StartsWith("cosmetic:", StringComparison.OrdinalIgnoreCase)))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_LOADOUT_ITEM", "Cosmetic item types must start with 'cosmetic:'.");

            var ownedItems = await database.PlayerTransactions
                .AsNoTracking()
                .Where(t => t.Status == PlayerTransactionStatus.Applied
                            && t.Actors.Any(a => a.PlayerId == userId))
                .SelectMany(t => t.ItemChanges)
                .Where(i => IsLoadoutItemType(i.ItemType))
                .GroupBy(i => i.ItemType.ToLower())
                .Select(g => new
                {
                    ItemType = g.Key,
                    Quantity = g.Sum(i => i.Operation == ItemOperation.Revoke ? -i.Quantity : i.Quantity)
                })
                .Where(x => x.Quantity > 0)
                .ToListAsync(ct);

            var ownedSet = ownedItems.Select(x => x.ItemType).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (desiredAvatar is not null && !ownedSet.Contains(desiredAvatar))
                return ApiResponses.Error(StatusCodes.Status409Conflict, "LOADOUT_ITEM_NOT_OWNED", $"Avatar item '{desiredAvatar}' is not owned.");

            var missingCosmetics = desiredCosmetics.Where(c => !ownedSet.Contains(c)).ToArray();
            if (missingCosmetics.Length > 0)
                return ApiResponses.Error(StatusCodes.Status409Conflict, "LOADOUT_ITEM_NOT_OWNED", $"Cosmetic item '{missingCosmetics[0]}' is not owned.");

            var prefs = await database.PlayerPreferences
                .FirstOrDefaultAsync(p => p.PlayerId == userId, ct);

            if (prefs is null)
            {
                prefs = new PlayerPreferences { PlayerId = userId };
                database.PlayerPreferences.Add(prefs);
            }

            prefs.AvatarItemType = desiredAvatar;
            prefs.EquippedCosmeticItemTypesCsv = string.Join(",", desiredCosmetics);
            prefs.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await database.SaveChangesAsync(ct);

            return Results.Ok(new PlayerLoadoutDto(
                prefs.AvatarItemType,
                desiredCosmetics));
        }

        private static bool IsLoadoutItemType(string itemType)
            => itemType.StartsWith("avatar:", StringComparison.OrdinalIgnoreCase)
               || itemType.StartsWith("cosmetic:", StringComparison.OrdinalIgnoreCase);

        private static IReadOnlyList<string> ParseCsv(string csv)
            => string.IsNullOrWhiteSpace(csv)
                ? Array.Empty<string>()
                : csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();
    }
}
