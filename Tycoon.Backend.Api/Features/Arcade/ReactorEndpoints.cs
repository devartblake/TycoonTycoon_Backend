using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Rewards;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.Backend.Api.Features.Arcade;

public static class ReactorEndpoints
{
    private static readonly TimeSpan SpinSessionTtl = TimeSpan.FromMinutes(5);

    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/arcade/reactor").WithTags("Arcade Reactor").RequireAuthorization();

        g.MapGet("/config", Config).WithName("ReactorConfig");
        g.MapPost("/spin", Spin).WithName("ReactorSpin");
        g.MapPost("/claim", Claim).WithName("ReactorClaim");
        g.MapPost("/chain", Chain).WithName("ReactorChain");
    }

    private static IResult Config(RewardReactorRuntimeContextService runtimeContext)
    {
        var config = runtimeContext.GetConfig();
        return Results.Ok(new ReactorConfigResponse(
            config.SeasonKey,
            config.SymbolSet,
            config.AssetBaseUrl));
    }

    private static async Task<IResult> Spin(
        ReactorSpinRequest request,
        HttpContext httpContext,
        AppDb db,
        RewardOutcomeService outcomeService,
        RewardPolicyService policyService,
        RewardReactorRuntimeContextService runtimeContext,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return RewardError(400, "REWARD_INVALID_REQUEST", "idempotencyKey is required.");

        // Idempotency: return existing pending session if same key was already used
        var existing = await db.RewardSessions
            .FirstOrDefaultAsync(s =>
                s.PlayerId == playerId &&
                s.IdempotencyKey == request.IdempotencyKey, ct);

        if (existing is not null)
            return Results.Ok(BuildSpinResponse(existing));

        // Policy check
        var policy = await policyService.CheckAsync(playerId, RewardMechanism.Reactor, ct);
        if (!policy.Allowed)
            return RewardError(409, policy.ErrorCode!, policy.ErrorMessage!);

        // Select reward
        var entry = outcomeService.SelectFromPool(ReactorRewardPool.Entries);
        var runtime = runtimeContext.ResolveForSpin(DateTimeOffset.UtcNow);
        var effectiveLines = ApplyEventMultiplier(entry.Lines, runtime.EventMultiplier);

        var rewardLinesJson = JsonSerializer.Serialize(effectiveLines);
        var animationJson = JsonSerializer.Serialize(entry.Animation);
        var runtimeSnapshotJson = JsonSerializer.Serialize(new ReactorRuntimeSnapshot(
            runtime.EventId,
            runtime.EventMultiplier,
            runtime.SeasonKey));

        // Generate opaque claim token
        var (plainToken, tokenHash) = GenerateClaimToken();

        var session = RewardSession.Create(
            playerId,
            RewardMechanism.Reactor,
            entry.RewardId,
            rewardLinesJson,
            animationJson,
            request.IdempotencyKey,
            tokenHash,
            expiresAtUtc: DateTimeOffset.UtcNow + SpinSessionTtl,
            policySnapshotJson: runtimeSnapshotJson,
            reactorId: request.ReactorId);

        db.RewardSessions.Add(session);
        await db.SaveChangesAsync(ct);

        var response = BuildSpinResponse(
            session,
            plainToken,
            policy.CooldownUntilUtc,
            entry,
            runtime,
            computeFallbackCooldown: true);
        return Results.Ok(response);
    }

    private static async Task<IResult> Claim(
        ReactorClaimRequest request,
        HttpContext httpContext,
        RewardClaimService claimService,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.SpinId) || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return RewardError(400, "REWARD_INVALID_REQUEST", "spinId and idempotencyKey are required.");

        var result = await claimService.ClaimAsync(playerId, request.SpinId,
            request.IdempotencyKey, request.ClaimToken, ct);

        if (result.ErrorCode is not null)
        {
            var httpStatus = result.ErrorCode switch
            {
                "REWARD_PENDING_NOT_FOUND" => 404,
                "REWARD_PLAYER_MISMATCH" or "REWARD_INVALID_TOKEN" => 403,
                _ => 409
            };
            return RewardError(httpStatus, result.ErrorCode, result.ErrorMessage!);
        }

        var lines = result.Lines
            .Select(l => new ReactorRewardLineDto(l.Type, l.Amount))
            .ToList();

        return Results.Ok(new ReactorClaimResponse(
            result.SpinId,
            result.Status,
            result.Duplicate,
            result.AppliedAtUtc,
            lines,
            new ReactorWalletDto(result.WalletCoins, result.WalletDiamonds, result.WalletXp),
            ChainedSpinId: result.ChainedSpinId));
    }

    private static async Task<IResult> Chain(
        ReactorChainRequest request,
        HttpContext httpContext,
        AppDb db,
        RewardReactorRuntimeContextService runtimeContext,
        CancellationToken ct)
    {
        if (!TryGetPlayerId(httpContext, out var playerId))
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.ChainedSpinId))
            return RewardError(400, "REWARD_INVALID_REQUEST", "chainedSpinId is required.");

        var ticket = await db.RewardChainTickets
            .FirstOrDefaultAsync(t => t.ChainedSpinId == request.ChainedSpinId, ct);

        if (ticket is null)
            return RewardError(404, "REWARD_CHAIN_NOT_FOUND", "Chain ticket not found.");

        if (ticket.PlayerId != playerId)
            return RewardError(403, "REWARD_PLAYER_MISMATCH", "Reward chain belongs to another player.");

        if (ticket.IsExpired())
        {
            ticket.MarkExpired();
            await db.SaveChangesAsync(ct);
            return RewardError(409, "REWARD_CHAIN_EXPIRED", "Reward chain ticket has expired.");
        }

        if (!string.IsNullOrWhiteSpace(ticket.GeneratedSpinId))
        {
            var existing = await db.RewardSessions
                .FirstOrDefaultAsync(s => s.SpinId == ticket.GeneratedSpinId, ct);

            if (existing is null)
                return RewardError(409, "REWARD_CHAIN_INVALID", "Reward chain state is invalid.");

            return Results.Ok(BuildSpinResponse(
                existing,
                plainToken: ticket.GeneratedClaimToken ?? string.Empty,
                cooldownUntil: null,
                entry: null,
                computeFallbackCooldown: false));
        }

        var (plainToken, tokenHash) = GenerateClaimToken();
        var runtime = runtimeContext.ResolveForSpin(DateTimeOffset.UtcNow);
        var runtimeSnapshotJson = JsonSerializer.Serialize(new ReactorRuntimeSnapshot(
            runtime.EventId,
            runtime.EventMultiplier,
            runtime.SeasonKey));

        var session = RewardSession.Create(
            playerId,
            RewardMechanism.Reactor,
            ticket.RewardId,
            ticket.RewardLinesJson,
            ticket.AnimationJson,
            idempotencyKey: $"chain:{ticket.ChainedSpinId}",
            claimTokenHash: tokenHash,
            expiresAtUtc: ticket.ExpiresAtUtc,
            policySnapshotJson: runtimeSnapshotJson,
            reactorId: "chain-reactor");

        db.RewardSessions.Add(session);
        ticket.MarkActivated(session.SpinId, plainToken);

        await db.SaveChangesAsync(ct);

        return Results.Ok(BuildSpinResponse(
            session,
            plainToken: plainToken,
            cooldownUntil: null,
            entry: null,
            computeFallbackCooldown: false));
    }

    private static ReactorSpinResponse BuildSpinResponse(
        RewardSession session,
        string? plainToken = null,
        DateTimeOffset? cooldownUntil = null,
        RewardPoolEntry? entry = null,
        RewardReactorSpinRuntimeContext? runtime = null,
        bool computeFallbackCooldown = true)
    {
        var animation = JsonSerializer.Deserialize<RewardAnimationHint>(session.AnimationJson,
            JsonOpts) ?? new("three_reel_reactor", [], [], "common", "low");

        var lines = JsonSerializer.Deserialize<List<RewardLine>>(session.RewardLinesJson,
            JsonOpts) ?? [];
        var snapshot = runtime ?? TryGetRuntimeSnapshot(session.PolicySnapshotJson);

        // Compute cooldown from session expiry when reconstructing from DB (idempotent re-return)
        var cooldown = cooldownUntil;
        if (cooldown is null && computeFallbackCooldown)
            cooldown = new DateTimeOffset(session.CreatedAtUtc.Date.AddDays(1), TimeSpan.Zero);

        return new ReactorSpinResponse(
            session.SpinId,
            session.Status.ToString(),
            session.ExpiresAtUtc,
            cooldown,
            new ReactorAnimationDto(animation.Layout, animation.Symbols,
                animation.WinningSymbolIndexes, animation.Rarity, animation.Intensity),
            new ReactorRewardPreviewDto(
                session.RewardId,
                entry?.DisplayName ?? session.RewardId,
                lines.Select(l => new ReactorRewardLineDto(l.Type, l.Amount)).ToList()),
            plainToken ?? string.Empty, // not re-returned on idempotent re-fetch for security
            EventId: snapshot?.EventId,
            EventMultiplier: snapshot?.EventMultiplier,
            SeasonKey: snapshot?.SeasonKey
        );
    }

    private static IReadOnlyList<RewardLine> ApplyEventMultiplier(
        IReadOnlyList<RewardLine> lines,
        double? eventMultiplier)
    {
        if (eventMultiplier is null || eventMultiplier <= 0 || Math.Abs(eventMultiplier.Value - 1.0) < 0.0001)
            return lines;

        return lines
            .Select(l => l with
            {
                Amount = Math.Max(1, (int)Math.Round(l.Amount * eventMultiplier.Value, MidpointRounding.AwayFromZero))
            })
            .ToList();
    }

    private static RewardReactorSpinRuntimeContext? TryGetRuntimeSnapshot(string? policySnapshotJson)
    {
        if (string.IsNullOrWhiteSpace(policySnapshotJson))
            return null;

        var snapshot = JsonSerializer.Deserialize<ReactorRuntimeSnapshot>(policySnapshotJson, JsonOpts);
        if (snapshot is null)
            return null;

        return new RewardReactorSpinRuntimeContext(
            snapshot.SeasonKey,
            snapshot.EventId,
            snapshot.EventMultiplier);
    }

    private sealed record ReactorRuntimeSnapshot(
        string? EventId,
        double? EventMultiplier,
        string? SeasonKey);

    private static (string Token, string Hash) GenerateClaimToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes);
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        return (token, hash);
    }

    private static IResult RewardError(int status, string code, string message)
        => Results.Json(new { error = new { code, message } }, statusCode: status);

    private static bool TryGetPlayerId(HttpContext ctx, out Guid playerId)
    {
        playerId = Guid.Empty;
        var claim = ctx.User.FindFirst(ClaimTypes.NameIdentifier) ?? ctx.User.FindFirst("sub");
        return claim is not null && Guid.TryParse(claim.Value, out playerId) && playerId != Guid.Empty;
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
}

public sealed record ReactorConfigResponse(
    string? SeasonKey,
    string? SymbolSet,
    string? AssetBaseUrl
);
