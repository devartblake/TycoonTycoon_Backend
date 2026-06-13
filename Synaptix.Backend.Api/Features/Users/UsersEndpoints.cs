using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Economy;
using Synaptix.Backend.Application.Media;
using Synaptix.Backend.Application.Notifications;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Users
{
    public static class UsersEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var usersGroup = app.MapGroup("/users")
                .WithTags("Users")
                .RequireAuthorization();

            usersGroup.MapGet("/search", SearchUsers);
            usersGroup.MapGet("/me", GetCurrentUserProfile);
            usersGroup.MapGet("/{userId:guid}/career-summary", GetCareerSummary);
            usersGroup.MapPost("/me/avatar/upload-url", CreateAvatarUploadUrl);
            usersGroup.MapPatch("/me", UpdateCurrentUserProfile);
            usersGroup.MapGet("/me/wallet", GetMyWallet);
            usersGroup.MapGet("/me/transactions", GetMyTransactions);
            usersGroup.MapPost("/me/onboarding-reward", ClaimOnboardingReward);
        }

        private static async Task<IResult> GetCurrentUserProfile(
            HttpContext httpContext,
            IAppDb database,
            CancellationToken cancellation)
        {
            if (!TryGetUserId(httpContext, out var userId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var currentUser = await database.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellation);

            if (currentUser is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "User not found.");

            return Results.Ok(new UserDto(
                currentUser.Id,
                currentUser.Handle,
                currentUser.Email,
                currentUser.Country,
                currentUser.AvatarUrl,
                currentUser.Tier,
                currentUser.Mmr,
                currentUser.SystemRole is not null ? [currentUser.SystemRole] : null
            ));
        }

        private static async Task<IResult> SearchUsers(
            [FromQuery] string handle,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IAppDb database,
            CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(handle) || handle.Trim().Length < 2)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Query parameter 'handle' must be at least 2 characters.");

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 20 : Math.Clamp(pageSize, 1, 50);

            var normalizedHandle = handle.Trim().ToLowerInvariant();

            var query = database.Users
                .AsNoTracking()
                .Where(u => u.Handle.ToLower().Contains(normalizedHandle))
                .OrderBy(u => u.Handle);

            var total = await query.CountAsync(cancellation);
            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserSearchResultDto(
                    u.Id,
                    u.Handle,
                    u.Handle,
                    u.Handle,
                    u.AvatarUrl,
                    u.Country,
                    u.Tier,
                    u.Mmr
                ))
                .ToListAsync(cancellation);

            return Results.Ok(new UserSearchResponseDto(page, pageSize, total, totalPages, users));
        }

        private static async Task<IResult> CreateAvatarUploadUrl(
            [FromBody] AvatarUploadUrlRequest request,
            HttpContext httpContext,
            MediaService mediaService,
            CancellationToken cancellation)
        {
            if (!TryGetUserId(httpContext, out var userId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            if (string.IsNullOrWhiteSpace(request.FileName))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "fileName is required.");

            if (string.IsNullOrWhiteSpace(request.ContentType))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "contentType is required.");

            if (request.ContentLength <= 0)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "contentLength must be greater than zero.");

            var sanitizedFileName = request.FileName.Trim();
            var objectKey = $"avatars/{userId:D}/{DateTimeOffset.UtcNow:yyyyMMdd}/{Guid.NewGuid():N}_{sanitizedFileName}";
            var intent = await mediaService.CreateUploadIntentForAssetKeyAsync(
                objectKey,
                request.ContentType.Trim(),
                cancellation);

            var publicUrl = mediaService.GetPublicUrl(intent.AssetKey);
            return Results.Ok(new AvatarUploadUrlResponse(intent.UploadUrl, intent.AssetKey, publicUrl));
        }

        private static async Task<IResult> UpdateCurrentUserProfile(
            [FromBody] UpdateProfileRequest request,
            HttpContext httpContext,
            IAppDb database,
            CancellationToken cancellation)
        {
            if (!TryGetUserId(httpContext, out var parsedUserId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var currentUser = await database.Users
                .FirstOrDefaultAsync(u => u.Id == parsedUserId, cancellation);

            if (currentUser is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "User not found.");

            currentUser.UpdateProfile(request.Handle, request.Country, request.AvatarUrl);
            await database.SaveChangesAsync(cancellation);

            var updatedProfile = new UserDto(
                currentUser.Id,
                currentUser.Handle,
                currentUser.Email,
                currentUser.Country,
                currentUser.AvatarUrl,
                currentUser.Tier,
                currentUser.Mmr,
                currentUser.SystemRole is not null ? [currentUser.SystemRole] : null
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

        private static async Task<IResult> GetMyWallet(
            HttpContext httpContext,
            IAppDb database,
            CancellationToken cancellation)
        {
            if (!TryGetUserId(httpContext, out var userId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var wallet = await database.PlayerWallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.PlayerId == userId, cancellation);

            return Results.Ok(new PlayerWalletDto(
                PlayerId: userId,
                Credits: wallet?.Coins ?? 0,
                NeuralXp: wallet?.Xp ?? 0,
                SynapseShards: wallet?.Diamonds ?? 0,
                UpdatedAtUtc: wallet?.UpdatedAtUtc ?? DateTimeOffset.UtcNow));
        }

        private static async Task<IResult> GetMyTransactions(
            HttpContext httpContext,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            EconomyService economyService,
            CancellationToken cancellation)
        {
            if (!TryGetUserId(httpContext, out var userId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 20 : Math.Clamp(pageSize, 1, 100);

            var history = await economyService.GetHistoryAsync(userId, page, pageSize, cancellation);
            return Results.Ok(history);
        }

        private static async Task<IResult> ClaimOnboardingReward(
            HttpContext httpContext,
            IAppDb database,
            EconomyService economyService,
            PlayerInboxService inboxService,
            CancellationToken cancellation)
        {
            if (!TryGetUserId(httpContext, out var userId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var alreadyClaimed = await database.EconomyTransactions
                .AsNoTracking()
                .AnyAsync(t => t.PlayerId == userId && t.Kind == "onboarding-reward", cancellation);

            if (alreadyClaimed)
                return ApiResponses.Error(StatusCodes.Status409Conflict, "ALREADY_CLAIMED", "Onboarding reward has already been claimed.");

            // Deterministic EventId — same player always produces same ID, preventing double-grants on retries.
            var eventId = new Guid(System.Security.Cryptography.MD5.HashData(
                System.Text.Encoding.UTF8.GetBytes($"onboarding-reward:{userId}")));

            var result = await economyService.ApplyAsync(new CreateEconomyTxnRequest(
                EventId: eventId,
                PlayerId: userId,
                Kind: "onboarding-reward",
                Lines: new[]
                {
                    new EconomyLineDto(CurrencyType.Coins, 500),
                    new EconomyLineDto(CurrencyType.Xp, 100)
                },
                Note: "Welcome to Synaptix — starter Credits and Neural XP granted."
            ), cancellation);

            if (result.Status == EconomyTxnStatus.Duplicate)
                return ApiResponses.Error(StatusCodes.Status409Conflict, "ALREADY_CLAIMED", "Onboarding reward has already been claimed.");

            await inboxService.CreateAsync(
                userId,
                "system",
                "Onboarding reward claimed",
                "Your starter Credits and Neural XP are now available in your wallet.",
                "/wallet",
                new Dictionary<string, object?>
                {
                    ["kind"] = "onboarding-reward",
                    ["creditsGranted"] = 500,
                    ["neuralXpGranted"] = 100
                },
                "redeem",
                null,
                cancellation);

            return Results.Ok(new OnboardingRewardDto(
                PlayerId: userId,
                CreditsGranted: 500,
                NeuralXpGranted: 100,
                BalanceCredits: result.BalanceCoins,
                BalanceNeuralXp: result.BalanceXp,
                BalanceSynapseShards: result.BalanceDiamonds));
        }

        private static bool TryGetUserId(HttpContext httpContext, out Guid userId)
        {
            userId = Guid.Empty;
            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                        ?? httpContext.User.FindFirst("sub");
            return claim is not null && Guid.TryParse(claim.Value, out userId) && userId != Guid.Empty;
        }

        public sealed record PlayerWalletDto(
            Guid PlayerId,
            int Credits,
            int NeuralXp,
            int SynapseShards,
            DateTimeOffset UpdatedAtUtc);

        public sealed record OnboardingRewardDto(
            Guid PlayerId,
            int CreditsGranted,
            int NeuralXpGranted,
            int BalanceCredits,
            int BalanceNeuralXp,
            int BalanceSynapseShards);
    }
}
