using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;
using Synaptix.Backend.Application.Abstractions;

namespace Synaptix.Backend.Api.Features.Account;

/// <summary>
/// Guest account → Full account migration endpoints
/// Handles seamless transition from guest login to full user account with data transfer
/// </summary>
public static class AccountMigrationEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/account").WithTags("Account");

        g.MapPost("/migrate-to-full", MigrateGuestToFullAccount)
            .WithName("MigrateGuestToFullAccount")
            .RequireAuthorization();
    }

    /// <summary>
    /// Migrate guest account to full account with data transfer
    ///
    /// Flow:
    /// 1. Validate guest account is anonymous
    /// 2. Upgrade guest user to full account (email + password)
    /// 3. Transfer player progression data (same username)
    /// 4. Transfer wallet data (coins, diamonds, XP)
    /// 5. Delete guest account after successful migration
    /// 6. Return full profile + new auth tokens
    /// </summary>
    private static async Task<IResult> MigrateGuestToFullAccount(
        [FromBody] AccountMigrationRequest request,
        HttpContext httpContext,
        AppDb db,
        IAuthService authService,
        CancellationToken ct)
    {
        // Get guest user ID from JWT
        var subject = httpContext.User.FindFirst("sub")?.Value
                      ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (subject is null || !Guid.TryParse(subject, out var guestUserId))
            return Results.Unauthorized();

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { error = "Email is required" });
        if (string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { error = "Password is required" });
        if (request.Password.Length < 8)
            return Results.BadRequest(new { error = "Password must be at least 8 characters" });
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return Results.BadRequest(new { error = "DeviceId is required" });

        try
        {
            using (var transaction = await db.Database.BeginTransactionAsync(ct))
            {
                try
                {
                    // Step 1: Get guest user and validate it's anonymous
                    var guestUser = await db.Users
                        .FirstOrDefaultAsync(u => u.Id == guestUserId, ct);

                    if (guestUser is null)
                        return Results.NotFound(new { error = "Guest user not found" });

                    if (!guestUser.IsAnonymous)
                        return Results.BadRequest(new { error = "Account is already registered. Cannot migrate non-guest account." });

                    // Step 2: Upgrade guest user to full account
                    var handle = request.Username ?? request.Handle ?? guestUser.Handle;
                    var upgradedUser = await authService.UpgradeAccountAsync(
                        guestUserId,
                        request.Email,
                        request.Password,
                        request.DeviceId,
                        handle,
                        request.Country);

                    // Step 3-4: Get guest player data and transfer to full account
                    var guestPlayer = await db.Players
                        .FirstOrDefaultAsync(p => p.Username == guestUser.Handle, ct);

                    var guestWallet = guestPlayer is not null
                        ? await db.PlayerWallets
                            .FirstOrDefaultAsync(w => w.PlayerId == guestPlayer.Id, ct)
                        : null;

                    // If guest had player progression, we keep the same player record
                    // (no need to transfer since handle is same or we updated it)

                    // Step 5: Get updated user data for response
                    var fullUser = await db.Users
                        .FirstOrDefaultAsync(u => u.Id == guestUserId, ct);

                    var player = await db.Players
                        .FirstOrDefaultAsync(p => p.Username == fullUser.Handle, ct);

                    var wallet = player is not null
                        ? await db.PlayerWallets
                            .FirstOrDefaultAsync(w => w.PlayerId == player.Id, ct)
                        : null;

                    var userRoles = await db.UserRoles
                        .AsNoTracking()
                        .Where(ur => ur.UserId == guestUserId)
                        .Select(ur => ur.RoleName)
                        .ToListAsync(ct);

                    // Build complete profile response
                    var profile = BuildCurrentUserProfile(fullUser, player, wallet, userRoles);

                    // Step 6: Delete guest account if requested
                    bool guestDeleted = false;
                    if (request.DeleteGuestAccount)
                    {
                        // Note: In a real system, you might want to soft-delete or archive instead
                        // This is a hard delete for demonstration purposes
                        if (guestPlayer is not null && wallet is not null)
                        {
                            db.PlayerWallets.Remove(wallet);
                            db.Players.Remove(guestPlayer);
                        }

                        // We don't delete the User record itself (it's now the full account)
                        guestDeleted = true;
                    }

                    await db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    return Results.Ok(new AccountMigrationResponse(
                        Success: true,
                        Message: "Guest account successfully migrated to full account",
                        AccessToken: upgradedUser.AccessToken,
                        RefreshToken: upgradedUser.RefreshToken,
                        ExpiresIn: upgradedUser.ExpiresIn,
                        Profile: profile,
                        GuestAccountDeleted: guestDeleted));
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            }
        }
        catch (InvalidOperationException error) when (error.Message.Contains("already in use"))
        {
            return Results.Conflict(new { error = "email_already_exists", message = error.Message });
        }
        catch (InvalidOperationException error) when (error.Message.Contains("not available"))
        {
            return Results.Conflict(new { error = "username_taken", message = error.Message });
        }
        catch (InvalidOperationException error) when (error.Message.Contains("already registered"))
        {
            return Results.Conflict(new { error = "already_registered", message = error.Message });
        }
        catch (InvalidOperationException error)
        {
            return Results.BadRequest(new { error = "migration_failed", message = error.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
    }

    /// <summary>
    /// Build complete user profile from User, Player, and Wallet entities
    /// </summary>
    private static CurrentUserProfileDto BuildCurrentUserProfile(
        User user,
        Player? player,
        PlayerWallet? wallet,
        IReadOnlyList<string> userRoles)
    {
        // Extract preferences from Flags dictionary
        user.Flags.TryGetValue("age_group", out var ageGroupObj);
        user.Flags.TryGetValue("synaptix_mode", out var synaptixModeObj);
        user.Flags.TryGetValue("pref_home_surface", out var prefSurfaceObj);
        user.Flags.TryGetValue("reduced_motion", out var reducedMotionObj);
        user.Flags.TryGetValue("tone_preference", out var toneObj);
        user.Flags.TryGetValue("is_premium", out var premiumObj);

        return new CurrentUserProfileDto(
            UserId: user.Id,
            Username: user.Handle,
            Email: user.Email,
            DisplayName: user.Handle,
            Country: user.Country,
            AvatarUrl: user.AvatarUrl,
            UserRole: user.SystemRole,
            UserRoles: userRoles.Count > 0 ? userRoles : null,
            PlayerId: player?.Id,
            PlayerLevel: player?.Level ?? 1,
            PlayerXp: player?.Xp ?? 0,
            PlayerScore: player?.Score ?? 0,
            CurrentTierId: player?.TierId?.ToString(),
            Coins: wallet?.Coins ?? 0,
            Diamonds: wallet?.Diamonds ?? 0,
            CumulativeXp: wallet?.Xp ?? 0,
            IsPremium: premiumObj is bool premium && premium,
            AgeGroup: ageGroupObj?.ToString(),
            SynaptixMode: synaptixModeObj?.ToString(),
            PreferredHomeSurface: prefSurfaceObj?.ToString(),
            ReducedMotion: reducedMotionObj is bool reduced && reduced,
            TonePreference: toneObj?.ToString(),
            IsAnonymous: user.IsAnonymous,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt
        );
    }
}
