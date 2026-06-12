using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Account;

public static class AccountRewardsEndpoints
{
    private const int DailyCoins = 100;

    private static readonly WeeklyRewardDay[] WeeklySchedule =
    [
        new(1, "coins",  100, 0, "Day 1 — 100 Credits"),
        new(2, "gems",     0, 5, "Day 2 — 5 Synapse Shards"),
        new(3, "coins",  200, 0, "Day 3 — 200 Credits"),
        new(4, "coins",  150, 0, "Day 4 — 150 Credits"),
        new(5, "gems",     0, 10, "Day 5 — 10 Synapse Shards"),
        new(6, "coins",  300, 0, "Day 6 — 300 Credits"),
        new(7, "coins",  500, 5, "Day 7 — 500 Credits + 5 Synapse Shards"),
    ];

    public static void Map(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/account/rewards")
            .WithTags("Account")
            .RequireAuthorization();

        g.MapGet("/status", GetStatus)
            .WithName("GetAccountRewardsStatus");

        g.MapPost("/claim", ClaimDaily)
            .WithName("ClaimAccountDailyReward");
    }

    private static async Task<IResult> GetStatus(
        HttpContext httpContext,
        AppDb db,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var claimedToday = await db.DailyRewardClaims
            .AnyAsync(c => c.PlayerId == playerId && c.ClaimDate == today, ct);

        DateTimeOffset? nextDailyClaimAt = claimedToday
            ? new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1), TimeSpan.Zero)
            : null;

        var streak = await db.WeeklyStreakStates
            .FirstOrDefaultAsync(s => s.PlayerId == playerId, ct);

        var cycleExpired = streak is not null &&
                           streak.CycleStartDate.AddDays(7) <= today;

        var claimedDays = (streak is null || cycleExpired)
            ? Array.Empty<int>()
            : streak.GetClaimedDays();

        var currentDay = (streak is null || cycleExpired) ? 1 : streak.CurrentDay;

        return Results.Ok(new AccountRewardsStatusResponse(
            CanClaimDaily: !claimedToday,
            NextDailyClaimAt: nextDailyClaimAt,
            DailyCoins: DailyCoins,
            CurrentWeeklyDay: currentDay,
            WeeklyClaimedDays: claimedDays,
            WeeklySchedule: WeeklySchedule));
    }

    private static async Task<IResult> ClaimDaily(
        HttpContext httpContext,
        AppDb db,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var alreadyClaimed = await db.DailyRewardClaims
            .AnyAsync(c => c.PlayerId == playerId && c.ClaimDate == today, ct);

        if (alreadyClaimed)
        {
            var nextClaimAt = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1), TimeSpan.Zero);
            return Results.Conflict(new DailyClaimResponse(
                Success: false,
                CoinsGranted: 0,
                NewBalance: 0,
                Message: "Daily reward already claimed.",
                NextClaimAt: nextClaimAt));
        }

        var wallet = await db.PlayerWallets
            .FirstOrDefaultAsync(w => w.PlayerId == playerId, ct);

        if (wallet is null)
        {
            wallet = new PlayerWallet(playerId);
            db.PlayerWallets.Add(wallet);
        }

        wallet.Apply(dxp: 0, dcoins: DailyCoins, ddiamonds: 0);
        db.DailyRewardClaims.Add(new DailyRewardClaim(playerId, today, DailyCoins));

        await db.SaveChangesAsync(ct);

        return Results.Ok(new DailyClaimResponse(
            Success: true,
            CoinsGranted: DailyCoins,
            NewBalance: wallet.Coins,
            Message: "Daily reward claimed.",
            NextClaimAt: new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1), TimeSpan.Zero)));
    }

    private static bool TryGetPlayerId(HttpContext httpContext, out Guid playerId)
    {
        playerId = Guid.Empty;
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirst("sub");
        return claim is not null && Guid.TryParse(claim.Value, out playerId) && playerId != Guid.Empty;
    }
}

public sealed record AccountRewardsStatusResponse(
    bool CanClaimDaily,
    DateTimeOffset? NextDailyClaimAt,
    int DailyCoins,
    int CurrentWeeklyDay,
    IReadOnlyList<int> WeeklyClaimedDays,
    IReadOnlyList<WeeklyRewardDay> WeeklySchedule
);
