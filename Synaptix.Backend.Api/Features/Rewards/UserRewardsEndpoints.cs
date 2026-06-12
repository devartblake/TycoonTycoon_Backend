using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;

namespace Synaptix.Backend.Api.Features.Rewards;

public static class UserRewardsEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/users/me/rewards", GetMyRewards)
            .WithTags("Rewards")
            .WithName("GetMyRewards")
            .RequireAuthorization();
    }

    private static async Task<IResult> GetMyRewards(
        HttpContext httpContext,
        AppDb db,
        CancellationToken ct)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirst("sub");

        if (claim is null || !Guid.TryParse(claim.Value, out var playerId) || playerId == Guid.Empty)
            return Results.Unauthorized();

        var utcNow = DateTimeOffset.UtcNow;

        var pending = await db.RewardSessions
            .Where(s => s.PlayerId == playerId &&
                        s.Status == RewardSessionStatus.PendingClaim &&
                        s.ExpiresAtUtc > utcNow)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(10)
            .Select(s => new PendingRewardDto(
                s.Mechanism.ToString(),
                s.SpinId,
                s.RewardId,
                s.ExpiresAtUtc))
            .ToListAsync(ct);

        var recentClaims = await db.RewardClaimLedger
            .Where(l => l.PlayerId == playerId && l.ClaimStatus == "Applied")
            .OrderByDescending(l => l.AppliedAtUtc)
            .Take(10)
            .Select(l => new RecentClaimDto(
                l.Mechanism.ToString(),
                l.RewardId,
                l.AppliedAtUtc))
            .ToListAsync(ct);

        return Results.Ok(new UserRewardsResponse(pending, recentClaims));
    }
}

public sealed record UserRewardsResponse(
    IReadOnlyList<PendingRewardDto> Pending,
    IReadOnlyList<RecentClaimDto> RecentClaims
);

public sealed record PendingRewardDto(
    string Mechanism,
    string SpinId,
    string RewardId,
    DateTimeOffset ExpiresAtUtc
);

public sealed record RecentClaimDto(
    string Mechanism,
    string RewardId,
    DateTimeOffset ClaimedAtUtc
);
