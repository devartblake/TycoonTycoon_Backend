using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.Backend.Api.Features.Arcade;

public static class ArcadeSpinEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/arcade/spin").WithTags("Arcade Spin");

        g.MapGet("/segments", GetSegments)
            .WithName("GetSpinSegments");

        g.MapPost("/claim", ClaimReward)
            .WithName("ClaimSpinReward")
            .RequireAuthorization();
    }

    private static IResult GetSegments()
    {
        return Results.Ok(SpinRewardCatalog.GetEnabledSegments());
    }

    private static async Task<IResult> ClaimReward(
        SpinClaimRequest request,
        HttpContext httpContext,
        AppDb db,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.SegmentId) || string.IsNullOrWhiteSpace(request.SpinId))
        {
            return Results.BadRequest(new SpinClaimResponse(
                Success: false,
                CoinsGranted: 0,
                NewBalance: 0,
                Message: "segmentId and spinId are required."));
        }

        var alreadyClaimed = await db.SpinClaims
            .AnyAsync(c => c.SpinId == request.SpinId, ct);

        if (alreadyClaimed)
        {
            return Results.Conflict(new SpinClaimResponse(
                Success: false,
                CoinsGranted: 0,
                NewBalance: 0,
                Message: "Spin reward already claimed."));
        }

        var segment = SpinRewardCatalog.Find(request.SegmentId);

        if (segment is null)
        {
            return Results.BadRequest(new SpinClaimResponse(
                Success: false,
                CoinsGranted: 0,
                NewBalance: 0,
                Message: "Invalid spin segment."));
        }

        if (!segment.IsEnabled ||
            (segment.EnabledUntil is not null && segment.EnabledUntil <= DateTimeOffset.UtcNow))
        {
            return Results.BadRequest(new SpinClaimResponse(
                Success: false,
                CoinsGranted: 0,
                NewBalance: 0,
                Message: "Spin segment is disabled or expired."));
        }

        var wallet = await db.PlayerWallets
            .FirstOrDefaultAsync(w => w.PlayerId == playerId, ct);

        if (wallet is null)
        {
            wallet = new PlayerWallet(playerId);
            db.PlayerWallets.Add(wallet);
        }

        wallet.Apply(dxp: 0, dcoins: segment.Reward, ddiamonds: 0);

        var claim = new SpinClaim(playerId, request.SegmentId, request.SpinId, segment.Reward);
        db.SpinClaims.Add(claim);

        await db.SaveChangesAsync(ct);

        return Results.Ok(new SpinClaimResponse(
            Success: true,
            CoinsGranted: segment.Reward,
            NewBalance: wallet.Coins,
            Message: "Reward claimed."));
    }

    private static bool TryGetPlayerId(HttpContext httpContext, out Guid playerId)
    {
        playerId = Guid.Empty;
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirst("sub");
        return claim is not null && Guid.TryParse(claim.Value, out playerId) && playerId != Guid.Empty;
    }
}
