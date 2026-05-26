using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Application.Rewards;

public sealed class RewardClaimService
{
    private readonly IAppDb _db;
    private readonly RewardOutcomeService _outcomeService;
    private static readonly TimeSpan ChainTicketTtl = TimeSpan.FromMinutes(5);

    public RewardClaimService(IAppDb db, RewardOutcomeService outcomeService)
    {
        _db = db;
        _outcomeService = outcomeService;
    }

    public async Task<RewardClaimResult> ClaimAsync(
        Guid playerId,
        string spinId,
        string idempotencyKey,
        string? claimToken,
        CancellationToken ct)
    {
        var existingLedger = await _db.RewardClaimLedger
            .FirstOrDefaultAsync(l =>
                l.PlayerId == playerId &&
                l.IdempotencyKey == idempotencyKey, ct);

        if (existingLedger is not null)
        {
            var dupLines = JsonSerializer.Deserialize<List<RewardLine>>(existingLedger.RewardLinesJson,
                JsonOptions) ?? [];
            var wallet = await GetOrCreateWallet(playerId, ct);
            var chainedSpinId = await GetExistingChainedSpinIdAsync(playerId, spinId, ct);
            return new RewardClaimResult(spinId, "Applied", Duplicate: true,
                existingLedger.AppliedAtUtc, dupLines,
                wallet.Coins, wallet.Diamonds, wallet.Xp,
                chainedSpinId);
        }

        var session = await _db.RewardSessions
            .FirstOrDefaultAsync(s => s.SpinId == spinId, ct);

        if (session is null)
            return Error(spinId, "REWARD_PENDING_NOT_FOUND", "Reward session not found.");

        if (session.PlayerId != playerId)
            return Error(spinId, "REWARD_PLAYER_MISMATCH", "Reward belongs to another player.");

        if (!session.IsPendingClaim())
        {
            if (session.Status == RewardSessionStatus.Applied)
            {
                var dupLines = JsonSerializer.Deserialize<List<RewardLine>>(session.RewardLinesJson,
                    JsonOptions) ?? [];
                var w = await GetOrCreateWallet(playerId, ct);
                var chainedSpinId = await GetExistingChainedSpinIdAsync(playerId, spinId, ct);
                return new RewardClaimResult(spinId, "Applied", Duplicate: true,
                    session.ClaimedAtUtc ?? session.CreatedAtUtc, dupLines,
                    w.Coins, w.Diamonds, w.Xp,
                    chainedSpinId);
            }
            if (session.IsExpired() || session.Status == RewardSessionStatus.Expired)
                return Error(spinId, "REWARD_PENDING_EXPIRED", "Pending reward has expired.");

            return Error(spinId, "REWARD_REJECTED", "Reward session is not claimable.");
        }

        if (session.IsExpired())
        {
            session.MarkExpired();
            await _db.SaveChangesAsync(ct);
            return Error(spinId, "REWARD_PENDING_EXPIRED", "Pending reward has expired.");
        }

        if (session.ClaimTokenHash is not null)
        {
            if (claimToken is null || !VerifyToken(claimToken, session.ClaimTokenHash))
                return Error(spinId, "REWARD_INVALID_TOKEN", "Claim token is invalid.");
        }

        var lines = JsonSerializer.Deserialize<List<RewardLine>>(session.RewardLinesJson,
            JsonOptions) ?? [];

        var playerWallet = await GetOrCreateWallet(playerId, ct);

        var dxp = lines.Where(l => l.Type == "xp").Sum(l => l.Amount);
        var dcoins = lines.Where(l => l.Type == "coins").Sum(l => l.Amount);
        var ddiamonds = lines.Where(l => l.Type == "diamonds").Sum(l => l.Amount);

        playerWallet.Apply(dxp, dcoins, ddiamonds);

        session.MarkApplied();

        var ledger = RewardClaimLedger.Create(
            playerId, session.Mechanism, spinId, session.RewardId,
            session.RewardLinesJson, "Applied", idempotencyKey);
        _db.RewardClaimLedger.Add(ledger);

        var chainedSpinIdForResponse = await TryCreateChainTicketAsync(playerId, session, ct);

        await _db.SaveChangesAsync(ct);

        return new RewardClaimResult(spinId, "Applied", Duplicate: false,
            session.ClaimedAtUtc!.Value, lines,
            playerWallet.Coins, playerWallet.Diamonds, playerWallet.Xp,
            chainedSpinIdForResponse);
    }

    private async Task<string?> TryCreateChainTicketAsync(Guid playerId, RewardSession sourceSession, CancellationToken ct)
    {
        if (sourceSession.Mechanism != RewardMechanism.Reactor)
            return null;

        var existing = await _db.RewardChainTickets
            .FirstOrDefaultAsync(t =>
                t.PlayerId == playerId &&
                t.SourceSpinId == sourceSession.SpinId, ct);

        if (existing is not null)
        {
            if (existing.Status == RewardChainTicketStatus.Pending && existing.IsExpired())
                existing.MarkExpired();

            return existing.ChainedSpinId;
        }

        var animation = JsonSerializer.Deserialize<RewardAnimationHint>(sourceSession.AnimationJson, JsonOptions);
        if (animation is null)
            return null;

        var isEligible = string.Equals(animation.Rarity, "rare", StringComparison.OrdinalIgnoreCase)
            || string.Equals(animation.Rarity, "legendary", StringComparison.OrdinalIgnoreCase);

        if (!isEligible)
            return null;

        var chainedEntry = _outcomeService.SelectFromPool(ReactorRewardPool.Entries);
        var ticket = RewardChainTicket.Create(
            playerId,
            sourceSession.SpinId,
            chainedEntry.RewardId,
            JsonSerializer.Serialize(chainedEntry.Lines),
            JsonSerializer.Serialize(chainedEntry.Animation),
            DateTimeOffset.UtcNow + ChainTicketTtl);

        _db.RewardChainTickets.Add(ticket);
        return ticket.ChainedSpinId;
    }

    private async Task<string?> GetExistingChainedSpinIdAsync(Guid playerId, string sourceSpinId, CancellationToken ct)
    {
        var ticket = await _db.RewardChainTickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.PlayerId == playerId && t.SourceSpinId == sourceSpinId, ct);

        return ticket?.ChainedSpinId;
    }

    private async Task<PlayerWallet> GetOrCreateWallet(Guid playerId, CancellationToken ct)
    {
        var wallet = await _db.PlayerWallets.FirstOrDefaultAsync(w => w.PlayerId == playerId, ct);
        if (wallet is null)
        {
            wallet = new PlayerWallet(playerId);
            _db.PlayerWallets.Add(wallet);
        }
        return wallet;
    }

    private static RewardClaimResult Error(string spinId, string code, string message)
        => new(spinId, "Error", false, DateTimeOffset.UtcNow, [], 0, 0, 0, null, code, message);

    private static bool VerifyToken(string token, string storedHash)
    {
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(storedHash));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
