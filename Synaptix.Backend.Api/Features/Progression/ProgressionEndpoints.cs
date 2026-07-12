using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Progression;

/// <summary>
/// Tier progression system endpoints.
/// Tracks XP-based tier progression for players.
/// </summary>
public static class ProgressionEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/progression").WithTags("Progression");

        g.MapGet("/tiers", GetTierDefinitions)
            .WithName("GetTierDefinitions");

        g.MapGet("/player/{userId:guid}", GetPlayerProgress)
            .WithName("GetPlayerProgress")
            .RequireAuthorization();

        g.MapPost("/xp/award", AwardXp)
            .WithName("AwardXp")
            .RequireAuthorization();
    }

    /// <summary>
    /// Get all tier definitions
    /// </summary>
    private static IResult GetTierDefinitions()
    {
        return Results.Ok(TierProgression.TierDefinitions);
    }

    /// <summary>
    /// Get player's current tier progression
    /// </summary>
    private static async Task<IResult> GetPlayerProgress(
        [FromRoute] Guid userId,
        HttpContext httpContext,
        AppDb db,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        // Players can only view their own progress
        if (playerId != userId)
            return Results.Forbid();

        var player = await db.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == playerId, ct);

        if (player is null)
            return Results.NotFound();

        var currentTier = TierProgression.GetTierForXp(player.Xp);
        var nextTier = TierProgression.GetNextTier(currentTier);

        var xpInCurrentTier = player.Xp - currentTier.MinXp;
        var xpNeededForNextTier = (nextTier?.MinXp ?? currentTier.MaxXp) - currentTier.MinXp;
        var progressPercentage = xpNeededForNextTier > 0
            ? (xpInCurrentTier / xpNeededForNextTier) * 100
            : 100;

        var progress = new PlayerTierProgress(
            CurrentTierId: currentTier.Id,
            CurrentTierName: currentTier.Name,
            CurrentLevel: player.Level,
            CurrentXp: player.Xp,
            XpInCurrentTier: xpInCurrentTier,
            XpNeededForNextTier: xpNeededForNextTier,
            ProgressPercentage: progressPercentage);

        return Results.Ok(progress);
    }

    /// <summary>
    /// Award XP to player and handle tier advancement
    /// </summary>
    private static async Task<IResult> AwardXp(
        [FromBody] AwardXpRequest request,
        HttpContext httpContext,
        AppDb db,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        // Players can only award XP to themselves
        if (playerId != request.UserId)
            return Results.Forbid();

        var player = await db.Players
            .FirstOrDefaultAsync(p => p.Id == playerId, ct);

        if (player is null)
            return Results.NotFound("Player not found");

        if (request.XpAmount <= 0)
            return Results.BadRequest("XP amount must be positive");

        var previousXp = player.Xp;
        var previousTier = TierProgression.GetTierForXp(previousXp);

        // Award XP
        player.AddXp(request.XpAmount);

        var newTier = TierProgression.GetTierForXp(player.Xp);
        var tierUpgraded = previousTier.Id != newTier.Id;

        // Update tier if changed
        if (tierUpgraded && newTier.Id != player.TierId?.ToString())
        {
            var tierEntity = await db.Tiers
                .FirstOrDefaultAsync(t => t.Name == newTier.Name, ct);
            if (tierEntity is not null)
            {
                player.SetTier(tierEntity.Id);
            }
        }

        await db.SaveChangesAsync(ct);

        var result = new XpAwardResult(
            XpAwarded: request.XpAmount,
            TotalXp: player.Xp,
            NewLevel: player.Level,
            TierUpgraded: tierUpgraded,
            NewTierId: tierUpgraded ? newTier.Id : null);

        return Results.Ok(result);
    }

    /// <summary>
    /// Extract player ID from JWT claims
    /// </summary>
    private static bool TryGetPlayerId(HttpContext httpContext, out Guid playerId)
    {
        playerId = Guid.Empty;
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && Guid.TryParse(claim.Value, out playerId);
    }
}

/// <summary>
/// Request body for awarding XP
/// </summary>
public record AwardXpRequest(
    Guid UserId,
    double XpAmount,
    string? Reason = null);
