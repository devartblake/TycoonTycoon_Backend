using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Spins;

public static class SpinsEndpoints
{
    private const int DefaultDailySpinLimit = 3;

    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/spins").WithTags("Spins");

        g.MapPost("/result", RecordSpinResult)
            .WithName("RecordSpinResult")
            .RequireAuthorization();

        g.MapGet("/stats/{userId:guid}", GetSpinStats)
            .WithName("GetSpinStats")
            .RequireAuthorization();

        g.MapGet("/history/{userId:guid}", GetSpinHistory)
            .WithName("GetSpinHistory")
            .RequireAuthorization();
    }

    private static async Task<IResult> RecordSpinResult(
        [FromBody] SpinResultRequest request,
        HttpContext httpContext,
        AppDb db,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.SpinId))
            return Results.BadRequest(new SpinResultResponse(false, 0, 0, 0, "spinId is required."));

        if (request.Amount < 0)
            return Results.BadRequest(new SpinResultResponse(false, 0, 0, 0, "Amount must be non-negative."));

        var alreadyClaimed = await db.SpinClaims
            .AnyAsync(c => c.SpinId == request.SpinId, ct);

        if (alreadyClaimed)
            return Results.Conflict(new SpinResultResponse(false, 0, 0, 0, "Spin result already recorded."));

        var isGems = string.Equals(request.RewardType, "gems", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(request.RewardType, "diamonds", StringComparison.OrdinalIgnoreCase);
        var dcoins = isGems ? 0 : request.Amount;
        var dgems = isGems ? request.Amount : 0;

        var wallet = await db.PlayerWallets
            .FirstOrDefaultAsync(w => w.PlayerId == playerId, ct);

        if (wallet is null)
        {
            wallet = new PlayerWallet(playerId);
            db.PlayerWallets.Add(wallet);
        }

        wallet.Apply(dxp: 0, dcoins: dcoins, ddiamonds: dgems);

        // Re-use SpinClaim entity so history queries work across both endpoints
        var claim = new SpinClaim(playerId, request.RewardType, request.SpinId, dcoins);
        db.SpinClaims.Add(claim);

        await db.SaveChangesAsync(ct);

        return Results.Ok(new SpinResultResponse(
            Success: true,
            CoinsGranted: dcoins,
            GemsGranted: dgems,
            NewBalance: wallet.Coins,
            Message: "Spin result recorded."));
    }

    private static async Task<IResult> GetSpinStats(
        [FromRoute] Guid userId,
        HttpContext httpContext,
        AppDb db,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        if (playerId != userId)
            return Results.Forbid();

        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var weekStart = todayStart.AddDays(-6);

        var claims = await db.SpinClaims
            .Where(c => c.PlayerId == playerId && c.ClaimedAtUtc >= weekStart)
            .Select(c => c.ClaimedAtUtc)
            .ToListAsync(ct);

        var dailyCount = claims.Count(c => c >= todayStart);
        var weeklyCount = claims.Count;
        var totalCount = await db.SpinClaims.CountAsync(c => c.PlayerId == playerId, ct);

        var remaining = Math.Max(0, DefaultDailySpinLimit - dailyCount);
        var dailyResetAt = todayStart.AddDays(1);

        return Results.Ok(new SpinStatsDto(
            DailyCount: dailyCount,
            WeeklyCount: weeklyCount,
            TotalCount: totalCount,
            DailyLimit: DefaultDailySpinLimit,
            RemainingToday: remaining,
            DailyResetAt: dailyResetAt));
    }

    private static async Task<IResult> GetSpinHistory(
        [FromRoute] Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        HttpContext httpContext = null!,
        AppDb db = null!,
        CancellationToken ct = default)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        if (playerId != userId)
            return Results.Forbid();

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var history = await db.SpinClaims
            .Where(c => c.PlayerId == playerId)
            .OrderByDescending(c => c.ClaimedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new SpinHistoryEntry(c.SpinId, c.SegmentId, c.CoinsGranted, c.ClaimedAtUtc))
            .ToListAsync(ct);

        return Results.Ok(history);
    }

    private static bool TryGetPlayerId(HttpContext httpContext, out Guid playerId)
    {
        playerId = Guid.Empty;
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirst("sub");
        return claim is not null && Guid.TryParse(claim.Value, out playerId) && playerId != Guid.Empty;
    }
}
