using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Rewards;

public static class RewardsEndpoints
{
    // Default reward amounts — configurable in future via admin endpoint
    private const int DailyCoins = 100;
    private const int DailyLimit = 1;

    private static readonly WeeklyRewardDay[] WeeklySchedule =
    [
        new(1, "coins",  100, 0, "Day 1 — 100 Credits"),
        new(2, "gems",     0, 5, "Day 2 — 5 Synapse Shards"),
        new(3, "coins",  200, 0, "Day 3 — 200 Credits"),
        new(4, "coins",  150, 0, "Day 4 — 150 Credits"),
        new(5, "gems",     0, 10,"Day 5 — 10 Synapse Shards"),
        new(6, "coins",  300, 0, "Day 6 — 300 Credits"),
        new(7, "coins",  500, 5, "Day 7 — 500 Credits + 5 Synapse Shards"),
    ];

    public static void Map(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/rewards").WithTags("Rewards");

        g.MapGet("/daily-config", GetDailyConfig)
            .AllowAnonymous()
            .WithName("GetDailyRewardConfig");

        g.MapPost("/daily/claim", ClaimDaily)
            .WithName("ClaimDailyReward")
            .RequireAuthorization();

        g.MapGet("/weekly-schedule", GetWeeklySchedule)
            .AllowAnonymous()
            .WithName("GetWeeklyRewardSchedule");

        g.MapGet("/weekly-streak/{userId:guid}", GetWeeklyStreak)
            .WithName("GetWeeklyStreak")
            .RequireAuthorization();

        g.MapPost("/weekly/claim", ClaimWeekly)
            .WithName("ClaimWeeklyReward")
            .RequireAuthorization();

        // Alternative endpoint path for Flutter compatibility
        g.MapPost("/weekly-streak/{userId:guid}/claim", ClaimWeeklyByUserId)
            .WithName("ClaimWeeklyRewardByUserId")
            .RequireAuthorization();

        g.MapGet("/spin-reward-steps", GetSpinRewardSteps)
            .AllowAnonymous()
            .WithName("GetSpinRewardSteps");
    }

    private static IResult GetDailyConfig()
    {
        return Results.Ok(new DailyRewardConfig(
            RewardType: "coins",
            CoinsAmount: DailyCoins,
            DisplayName: "Daily Mystery Box",
            IconName: "daily_box"));
    }

    private static IResult GetWeeklySchedule()
    {
        return Results.Ok(WeeklySchedule);
    }

    private static IResult GetSpinRewardSteps()
    {
        RewardStep[] steps =
        [
            new("coins",  50,  1,  "50 Credits"),
            new("coins",  100, 1,  "100 Credits"),
            new("coins",  200, 1,  "200 Credits"),
            new("gems",   5,   1,  "5 Synapse Shards"),
            new("coins",  500, 1,  "500 Credits"),
            new("gems",   10,  1,  "10 Synapse Shards"),
            new("coins",  1000,1,  "1,000 Credits"),
            new("gems",   25,  1,  "25 Synapse Shards"),
        ];
        return Results.Ok(steps);
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
            var nextClaimAt = DateTime.UtcNow.Date.AddDays(1);
            return Results.Conflict(new DailyClaimResponse(
                Success: false,
                CoinsGranted: 0,
                NewBalance: 0,
                Message: "Daily reward already claimed.",
                NextClaimAt: new DateTimeOffset(nextClaimAt, TimeSpan.Zero)));
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

    private static async Task<IResult> GetWeeklyStreak(
        [FromRoute] Guid userId,
        HttpContext httpContext,
        AppDb db,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        // Players can only view their own streak
        if (playerId != userId)
            return Results.Forbid();

        var streak = await db.WeeklyStreakStates
            .FirstOrDefaultAsync(s => s.PlayerId == playerId, ct);

        if (streak is null)
            return Results.Ok(BuildStreakData(null));

        // If the cycle started more than 7 days ago, the streak is expired
        var cycleExpired = streak.CycleStartDate.AddDays(7) <= DateOnly.FromDateTime(DateTime.UtcNow);
        if (cycleExpired)
            return Results.Ok(BuildStreakData(null));

        return Results.Ok(BuildStreakData(streak));
    }

    private static async Task<IResult> ClaimWeekly(
        [FromBody] WeeklyClaimRequest request,
        HttpContext httpContext,
        AppDb db,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        if (request.Day < 1 || request.Day > 7)
            return Results.BadRequest(new { error = "Day must be between 1 and 7." });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var streak = await db.WeeklyStreakStates
            .FirstOrDefaultAsync(s => s.PlayerId == playerId, ct);

        // Start a new cycle if none exists or current cycle expired
        if (streak is null)
        {
            streak = new WeeklyStreakState(playerId, today);
            db.WeeklyStreakStates.Add(streak);
        }
        else if (streak.CycleStartDate.AddDays(7) <= today)
        {
            streak.StartNewCycle(today);
        }

        var claimedDays = streak.GetClaimedDays();
        if (claimedDays.Contains(request.Day))
        {
            return Results.Conflict(new { error = $"Day {request.Day} already claimed in this cycle." });
        }

        var schedule = WeeklySchedule.First(s => s.Day == request.Day);
        var wallet = await db.PlayerWallets
            .FirstOrDefaultAsync(w => w.PlayerId == playerId, ct);

        if (wallet is null)
        {
            wallet = new PlayerWallet(playerId);
            db.PlayerWallets.Add(wallet);
        }

        wallet.Apply(dxp: 0, dcoins: schedule.CoinsAmount, ddiamonds: schedule.GemsAmount);
        streak.ClaimDay(request.Day);

        await db.SaveChangesAsync(ct);

        return Results.Ok(new WeeklyClaimResponse(
            Success: true,
            Day: request.Day,
            CoinsGranted: schedule.CoinsAmount,
            GemsGranted: schedule.GemsAmount,
            NewBalance: wallet.Coins,
            Message: $"Day {request.Day} reward claimed.",
            UpdatedStreak: BuildStreakData(streak)));
    }

    /// <summary>
    /// Alternative claim endpoint with userId in path (Flutter compatibility)
    /// </summary>
    private static async Task<IResult> ClaimWeeklyByUserId(
        [FromRoute] Guid userId,
        [FromBody] WeeklyClaimRequest request,
        HttpContext httpContext,
        AppDb db,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        // Players can only claim rewards for themselves
        if (playerId != userId)
            return Results.Forbid();

        if (request.Day < 1 || request.Day > 7)
            return Results.BadRequest(new { error = "Day must be between 1 and 7." });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var streak = await db.WeeklyStreakStates
            .FirstOrDefaultAsync(s => s.PlayerId == playerId, ct);

        // Start a new cycle if none exists or current cycle expired
        if (streak is null)
        {
            streak = new WeeklyStreakState(playerId, today);
            db.WeeklyStreakStates.Add(streak);
        }
        else if (streak.CycleStartDate.AddDays(7) <= today)
        {
            streak.StartNewCycle(today);
        }

        var claimedDays = streak.GetClaimedDays();
        if (claimedDays.Contains(request.Day))
        {
            return Results.Conflict(new { error = $"Day {request.Day} already claimed in this cycle." });
        }

        var schedule = WeeklySchedule.First(s => s.Day == request.Day);
        var wallet = await db.PlayerWallets
            .FirstOrDefaultAsync(w => w.PlayerId == playerId, ct);

        if (wallet is null)
        {
            wallet = new PlayerWallet(playerId);
            db.PlayerWallets.Add(wallet);
        }

        wallet.Apply(dxp: 0, dcoins: schedule.CoinsAmount, ddiamonds: schedule.GemsAmount);
        streak.ClaimDay(request.Day);

        await db.SaveChangesAsync(ct);

        return Results.Ok(new WeeklyClaimResponse(
            Success: true,
            Day: request.Day,
            CoinsGranted: schedule.CoinsAmount,
            GemsGranted: schedule.GemsAmount,
            NewBalance: wallet.Coins,
            Message: $"Day {request.Day} reward claimed.",
            UpdatedStreak: BuildStreakData(streak)));
    }

    private static WeeklyStreakData BuildStreakData(WeeklyStreakState? streak)
    {
        if (streak is null)
        {
            return new WeeklyStreakData(
                CurrentDay: 1,
                CycleStart: DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                ClaimedDays: [],
                Schedule: WeeklySchedule);
        }

        return new WeeklyStreakData(
            CurrentDay: streak.CurrentDay,
            CycleStart: streak.CycleStartDate.ToString("yyyy-MM-dd"),
            ClaimedDays: streak.GetClaimedDays(),
            Schedule: WeeklySchedule);
    }

    private static bool TryGetPlayerId(HttpContext httpContext, out Guid playerId)
    {
        playerId = Guid.Empty;
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirst("sub");
        return claim is not null && Guid.TryParse(claim.Value, out playerId) && playerId != Guid.Empty;
    }
}

internal record WeeklyClaimRequest(int Day);
