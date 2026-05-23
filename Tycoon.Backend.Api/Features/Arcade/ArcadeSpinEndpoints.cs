using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tycoon.Backend.Application.Rewards;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.Backend.Api.Features.Arcade;

public static class ArcadeSpinEndpoints
{
    private static readonly TimeSpan SpinSessionTtl = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/arcade/spin").WithTags("Arcade Spin");

        g.MapGet("/segments", GetSegments)
            .WithName("GetSpinSegments");

        g.MapPost("/start", StartSpin)
            .WithName("StartArcadeSpin")
            .RequireAuthorization();

        g.MapPost("/claim", ClaimReward)
            .WithName("ClaimSpinReward")
            .RequireAuthorization();
    }

    private static IResult GetSegments()
    {
        return Results.Ok(SpinRewardCatalog.GetEnabledSegments());
    }

    private static async Task<IResult> StartSpin(
        ArcadeSpinStartRequest request,
        HttpContext httpContext,
        AppDb db,
        RewardOutcomeService outcomeService,
        RewardPolicyService policyService,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return Results.BadRequest(new { error = new { code = "REWARD_INVALID_REQUEST", message = "idempotencyKey is required." } });

        // Idempotent re-return
        var existing = await db.RewardSessions
            .FirstOrDefaultAsync(s =>
                s.PlayerId == playerId &&
                s.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing is not null)
            return Results.Ok(BuildArcadeStartResponse(existing));

        // Policy check
        var policy = await policyService.CheckAsync(playerId, RewardMechanism.ArcadeSpin, ct);
        if (!policy.Allowed)
            return Results.Json(new { error = new { code = policy.ErrorCode, message = policy.ErrorMessage } }, statusCode: 409);

        // Select a server-side segment
        var entry = outcomeService.SelectFromPool(ReactorRewardPool.Entries);

        var (plainToken, tokenHash) = GenerateClaimToken();

        var session = RewardSession.Create(
            playerId,
            RewardMechanism.ArcadeSpin,
            entry.RewardId,
            JsonSerializer.Serialize(entry.Lines),
            JsonSerializer.Serialize(entry.Animation),
            request.IdempotencyKey,
            tokenHash,
            expiresAtUtc: DateTimeOffset.UtcNow + SpinSessionTtl);

        db.RewardSessions.Add(session);
        await db.SaveChangesAsync(ct);

        return Results.Ok(BuildArcadeStartResponse(session, plainToken, entry));
    }

    private static async Task<IResult> ClaimReward(
        SpinClaimRequest request,
        HttpContext httpContext,
        AppDb db,
        RewardClaimService claimService,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        // New contract: spinId + claimToken + idempotencyKey, server-authoritative reward from pending session.
        if (!string.IsNullOrWhiteSpace(request.SpinId) &&
            !string.IsNullOrWhiteSpace(request.ClaimToken) &&
            !string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var result = await claimService.ClaimAsync(
                playerId,
                request.SpinId,
                request.IdempotencyKey,
                request.ClaimToken,
                ct);

            if (result.ErrorCode is not null)
            {
                var status = result.ErrorCode switch
                {
                    "REWARD_PENDING_NOT_FOUND" => StatusCodes.Status404NotFound,
                    "REWARD_PLAYER_MISMATCH" or "REWARD_INVALID_TOKEN" => StatusCodes.Status403Forbidden,
                    _ => StatusCodes.Status409Conflict
                };

                return Results.Json(new SpinClaimResponse(
                    Success: false,
                    CoinsGranted: 0,
                    NewBalance: 0,
                    Message: result.ErrorMessage), statusCode: status);
            }

            var coinsGranted = result.Lines
                .Where(l => l.Type == "coins")
                .Sum(l => l.Amount);

            return Results.Ok(new SpinClaimResponse(
                Success: true,
                CoinsGranted: coinsGranted,
                NewBalance: result.WalletCoins,
                Message: result.Duplicate ? "Duplicate claim." : "Reward claimed."));
        }

        // Legacy compatibility during rollout: segmentId + spinId.
        if (string.IsNullOrWhiteSpace(request.SegmentId) || string.IsNullOrWhiteSpace(request.SpinId))
        {
            return Results.BadRequest(new SpinClaimResponse(
                Success: false,
                CoinsGranted: 0,
                NewBalance: 0,
                Message: "Provide either spinId+claimToken+idempotencyKey (new) or segmentId+spinId (legacy)."));
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

    private static ArcadeSpinStartResponse BuildArcadeStartResponse(
        RewardSession session,
        string? plainToken = null,
        RewardPoolEntry? entry = null)
    {
        var animation = JsonSerializer.Deserialize<RewardAnimationHint>(session.AnimationJson, JsonOpts)
            ?? new("wheel", [], [], "common", "low");

        var lines = JsonSerializer.Deserialize<List<RewardLine>>(session.RewardLinesJson, JsonOpts) ?? [];

        return new ArcadeSpinStartResponse(
            SpinId: session.SpinId,
            Status: session.Status.ToString(),
            ExpiresAtUtc: session.ExpiresAtUtc,
            RewardId: session.RewardId,
            Animation: new ArcadeSpinAnimationDto(
                animation.WinningSymbolIndexes.FirstOrDefault(),
                session.RewardId,
                animation.Rarity),
            RewardPreview: new ArcadeSpinRewardPreviewDto(
                session.RewardId,
                entry?.DisplayName ?? session.RewardId,
                lines.Select(l => new ReactorRewardLineDto(l.Type, l.Amount)).ToList()),
            ClaimToken: plainToken ?? string.Empty);
    }

    private static (string Token, string Hash) GenerateClaimToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes);
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        return (token, hash);
    }

    private static bool TryGetPlayerId(HttpContext httpContext, out Guid playerId)
    {
        playerId = Guid.Empty;
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirst("sub");
        return claim is not null && Guid.TryParse(claim.Value, out playerId) && playerId != Guid.Empty;
    }
}
