using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Api.Security;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Config;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Api.Features.Crypto;

public static class CryptoEconomyEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/crypto").WithTags("Crypto Economy")
            .AddEndpointFilter(async (ctx, next) =>
            {
                var flags = ctx.HttpContext.RequestServices.GetRequiredService<FeatureFlagService>();
                if (!await flags.IsEnabledAsync("crypto_enabled", ctx.HttpContext.RequestAborted))
                    return Results.Json(new { error = new { code = "FeatureDisabled", message = "This feature is not available in the current release.", details = new { } } }, statusCode: StatusCodes.Status403Forbidden);
                return await next(ctx);
            });

        g.MapPost("/link-wallet", LinkWallet).RequireAuthorization().RequireSecureChannel();
        g.MapGet("/balance/{playerId:guid}", GetBalance).RequireAuthorization();
        g.MapGet("/history/{playerId:guid}", GetHistory).RequireAuthorization();
        g.MapPost("/withdraw", RequestWithdrawal).RequireAuthorization().RequireSecureChannel();
        g.MapPost("/prize-pool/fund", FundPrizePool).RequireAuthorization();
        g.MapGet("/prize-pool/{poolId}", GetPrizePool).RequireAuthorization();
        g.MapPost("/prize-pool/distribute", DistributePrizePool).RequireAuthorization()
            .WithMetadata(new Security.RequireAdminOpsKeyAttribute());
        g.MapPost("/stake", Stake).RequireAuthorization().RequireSecureChannel();
        g.MapPost("/unstake", Unstake).RequireAuthorization().RequireSecureChannel();
        g.MapGet("/staking/{playerId:guid}", GetStakingPosition).RequireAuthorization();
        g.MapGet("/withdraw/pending", ListPendingWithdrawals).RequireAuthorization()
            .WithMetadata(new Security.RequireAdminOpsKeyAttribute());
        g.MapPost("/withdraw/{transactionId:guid}/approve", ApproveWithdrawal).RequireAuthorization()
            .WithMetadata(new Security.RequireAdminOpsKeyAttribute());
        g.MapPost("/withdraw/{transactionId:guid}/reject", RejectWithdrawal).RequireAuthorization()
            .WithMetadata(new Security.RequireAdminOpsKeyAttribute());
    }

    private static async Task<IResult> LinkWallet(
        [FromBody] LinkWalletRequest req,
        IAppDb db,
        IConfiguration cfg,
        CancellationToken ct)
    {
        if (!cfg.GetValue("Crypto:Enabled", false))
            return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "CRYPTO_DISABLED", "Crypto economy is disabled.");

        if (req.PlayerId == Guid.Empty || string.IsNullOrWhiteSpace(req.WalletAddress))
            return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId and walletAddress are required.");

        var normalized = req.WalletAddress.Trim();

        var tx = new PlayerTransaction(Guid.NewGuid(), "crypto-wallet-link", receipt: normalized);
        tx.AddActor(req.PlayerId, PlayerTransactionActorRole.Recipient);
        tx.MarkApplied();

        db.PlayerTransactions.Add(tx);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new LinkWalletResponse(req.PlayerId, normalized, req.Network, tx.Id, tx.Status.ToString()));
    }

    private static async Task<IResult> GetBalance(
        [FromRoute] Guid playerId,
        IAppDb db,
        CancellationToken ct)
    {
        if (playerId == Guid.Empty)
            return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId is required.");

        var applied = await db.PlayerTransactions.AsNoTracking()
            .Include(x => x.Actors)
            .Include(x => x.ItemChanges)
            .Where(x =>
                x.Status == PlayerTransactionStatus.Applied &&
                x.Actors.Any(a => a.PlayerId == playerId) &&
                x.ItemChanges.Any(i => i.ItemType == "crypto:units"))
            .ToListAsync(ct);

        var units = applied.SelectMany(x => x.ItemChanges)
            .Where(i => i.ItemType == "crypto:units")
            .Sum(i => i.Operation == ItemOperation.Grant ? i.Quantity : -i.Quantity);

        return Results.Ok(new CryptoBalanceResponse(playerId, Math.Max(0, units), "CRYPTO_UNITS"));
    }

    private static async Task<IResult> GetHistory(
        [FromRoute] Guid playerId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IAppDb db,
        CancellationToken ct)
    {
        if (playerId == Guid.Empty)
            return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId is required.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100);

        var q = db.PlayerTransactions.AsNoTracking()
            .Include(x => x.Actors)
            .Include(x => x.ItemChanges)
            .Where(x =>
                x.Kind.StartsWith("crypto-") &&
                x.Actors.Any(a => a.PlayerId == playerId))
            .OrderByDescending(x => x.CreatedAtUtc);

        var total = await q.CountAsync(ct);
        var rows = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var items = rows.Select(x =>
        {
            var amount = x.ItemChanges
                .Where(i => i.ItemType == "crypto:units")
                .Sum(i => i.Operation == ItemOperation.Grant ? i.Quantity : -i.Quantity);

            return new CryptoHistoryItem(
                x.Id,
                x.Kind,
                amount,
                x.Status.ToString(),
                x.Receipt,
                x.CreatedAtUtc,
                x.CompletedAtUtc
            );
        }).ToList();

        return Results.Ok(new CryptoHistoryResponse(page, pageSize, total, items));
    }

    private static async Task<IResult> RequestWithdrawal(
        [FromBody] CryptoWithdrawRequest req,
        IAppDb db,
        IConfiguration cfg,
        CancellationToken ct)
    {
        if (!cfg.GetValue("Crypto:Enabled", false))
            return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "CRYPTO_DISABLED", "Crypto economy is disabled.");

        if (req.PlayerId == Guid.Empty || req.Units <= 0 || string.IsNullOrWhiteSpace(req.ToWalletAddress))
            return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId, units, and toWalletAddress are required.");

        var minWithdrawal = cfg.GetValue("Crypto:MinWithdrawalUnits", 1);
        if (req.Units < minWithdrawal)
            return ApiResponses.Error(StatusCodes.Status400BadRequest, "MIN_WITHDRAWAL", $"Minimum withdrawal is {minWithdrawal} units.");

        var linked = await db.PlayerTransactions.AsNoTracking()
            .AnyAsync(x =>
                x.Kind == "crypto-wallet-link" &&
                x.Status == PlayerTransactionStatus.Applied &&
                x.Actors.Any(a => a.PlayerId == req.PlayerId), ct);

        if (!linked)
            return ApiResponses.Error(StatusCodes.Status409Conflict, "WALLET_NOT_LINKED", "Link wallet before requesting withdrawal.");

        var applied = await db.PlayerTransactions.AsNoTracking()
            .Include(x => x.Actors)
            .Include(x => x.ItemChanges)
            .Where(x =>
                x.Status == PlayerTransactionStatus.Applied &&
                x.Actors.Any(a => a.PlayerId == req.PlayerId) &&
                x.ItemChanges.Any(i => i.ItemType == "crypto:units"))
            .ToListAsync(ct);

        var balance = applied.SelectMany(x => x.ItemChanges)
            .Where(i => i.ItemType == "crypto:units")
            .Sum(i => i.Operation == ItemOperation.Grant ? i.Quantity : -i.Quantity);

        if (balance < req.Units)
            return ApiResponses.Error(StatusCodes.Status409Conflict, "INSUFFICIENT_CRYPTO_BALANCE", "Insufficient crypto balance.");

        var tx = new PlayerTransaction(Guid.NewGuid(), "crypto-withdraw-request", receipt: req.ToWalletAddress.Trim());
        tx.AddActor(req.PlayerId, PlayerTransactionActorRole.Sender);
        tx.AddItemChange("crypto:units", req.Units, ItemOperation.Revoke);
        // remains Pending until an approval worker applies settlement

        db.PlayerTransactions.Add(tx);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new CryptoWithdrawResponse(tx.Id, tx.Status.ToString(), req.Units, req.Network));
    }

    private static async Task<IResult> FundPrizePool(
        [FromBody] CryptoPrizePoolFundRequest req,
        IAppDb db,
        IConfiguration cfg,
        CancellationToken ct)
    {
        if (!cfg.GetValue("Crypto:Enabled", false))
            return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "CRYPTO_DISABLED", "Crypto economy is disabled.");

        if (req.PlayerId == Guid.Empty || req.Units <= 0)
            return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId and positive units are required.");

        var balance = await GetAppliedCryptoUnitsAsync(db, req.PlayerId, ct);
        if (balance < req.Units)
            return ApiResponses.Error(StatusCodes.Status409Conflict, "INSUFFICIENT_CRYPTO_BALANCE", "Insufficient crypto balance.");

        var poolId = NormalizePoolId(req.PoolId);
        var tx = new PlayerTransaction(Guid.NewGuid(), "crypto-prize-pool-fund", receipt: poolId);
        tx.AddActor(req.PlayerId, PlayerTransactionActorRole.Sender);
        tx.AddItemChange("crypto:units", req.Units, ItemOperation.Revoke);
        tx.AddItemChange("crypto:prize-pool:units", req.Units, ItemOperation.Grant);
        tx.MarkApplied();

        db.PlayerTransactions.Add(tx);
        await db.SaveChangesAsync(ct);

        var total = await GetPrizePoolUnitsAsync(db, poolId, ct);
        return Results.Ok(new CryptoPrizePoolFundResponse(tx.Id, poolId, req.Units, total, tx.Status.ToString()));
    }

    private static async Task<IResult> GetPrizePool(
        [FromRoute] string poolId,
        IAppDb db,
        CancellationToken ct)
    {
        var normalizedPoolId = NormalizePoolId(poolId);
        var units = await GetPrizePoolUnitsAsync(db, normalizedPoolId, ct);
        return Results.Ok(new CryptoPrizePoolBalanceResponse(normalizedPoolId, Math.Max(0, units), "CRYPTO_UNITS"));
    }

    private static async Task<IResult> DistributePrizePool(
        [FromBody] CryptoPrizePoolDistributeRequest req,
        IAppDb db,
        IConfiguration cfg,
        CancellationToken ct)
    {
        if (!cfg.GetValue("Crypto:Enabled", false))
            return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "CRYPTO_DISABLED", "Crypto economy is disabled.");

        var poolId = NormalizePoolId(req.PoolId);
        if (req.Winners.Count == 0 || req.Winners.Any(w => w.PlayerId == Guid.Empty || w.Units <= 0))
            return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "winners must include valid playerId and positive units.");

        var requestedUnits = req.Winners.Sum(w => w.Units);
        var availableUnits = await GetPrizePoolUnitsAsync(db, poolId, ct);
        if (availableUnits < requestedUnits)
            return ApiResponses.Error(StatusCodes.Status409Conflict, "INSUFFICIENT_PRIZE_POOL_BALANCE", "Prize pool has insufficient units.");

        var payoutTxs = new List<PlayerTransaction>();
        foreach (var winner in req.Winners)
        {
            var tx = new PlayerTransaction(Guid.NewGuid(), "crypto-prize-pool-payout", receipt: poolId);
            tx.AddActor(winner.PlayerId, PlayerTransactionActorRole.Recipient);
            tx.AddItemChange("crypto:prize-pool:units", winner.Units, ItemOperation.Revoke);
            tx.AddItemChange("crypto:units", winner.Units, ItemOperation.Grant);
            tx.MarkApplied();
            payoutTxs.Add(tx);
        }

        db.PlayerTransactions.AddRange(payoutTxs);
        await db.SaveChangesAsync(ct);

        var remaining = await GetPrizePoolUnitsAsync(db, poolId, ct);
        return Results.Ok(new CryptoPrizePoolDistributeResponse(
            poolId,
            requestedUnits,
            remaining,
            payoutTxs.Select(x => x.Id).ToList()
        ));
    }

    private static async Task<IResult> Stake(
        [FromBody] CryptoStakeRequest req,
        IAppDb db,
        IConfiguration cfg,
        CancellationToken ct)
    {
        if (!cfg.GetValue("Crypto:Enabled", false))
            return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "CRYPTO_DISABLED", "Crypto economy is disabled.");

        if (req.PlayerId == Guid.Empty || req.Units <= 0)
            return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId and positive units are required.");

        var balance = await GetAppliedCryptoUnitsAsync(db, req.PlayerId, ct);
        if (balance < req.Units)
            return ApiResponses.Error(StatusCodes.Status409Conflict, "INSUFFICIENT_CRYPTO_BALANCE", "Insufficient crypto balance.");

        var tx = new PlayerTransaction(Guid.NewGuid(), "crypto-stake-lock", receipt: req.StakeId?.Trim());
        tx.AddActor(req.PlayerId, PlayerTransactionActorRole.Sender);
        tx.AddItemChange("crypto:units", req.Units, ItemOperation.Revoke);
        tx.AddItemChange("crypto:staked:units", req.Units, ItemOperation.Grant);
        tx.MarkApplied();

        db.PlayerTransactions.Add(tx);
        await db.SaveChangesAsync(ct);

        var staked = await GetStakedUnitsAsync(db, req.PlayerId, ct);
        return Results.Ok(new CryptoStakeResponse(tx.Id, req.PlayerId, req.Units, staked, tx.Status.ToString()));
    }

    private static async Task<IResult> Unstake(
        [FromBody] CryptoStakeRequest req,
        IAppDb db,
        IConfiguration cfg,
        CancellationToken ct)
    {
        if (!cfg.GetValue("Crypto:Enabled", false))
            return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "CRYPTO_DISABLED", "Crypto economy is disabled.");

        if (req.PlayerId == Guid.Empty || req.Units <= 0)
            return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId and positive units are required.");

        var staked = await GetStakedUnitsAsync(db, req.PlayerId, ct);
        if (staked < req.Units)
            return ApiResponses.Error(StatusCodes.Status409Conflict, "INSUFFICIENT_STAKED_BALANCE", "Insufficient staked balance.");

        var tx = new PlayerTransaction(Guid.NewGuid(), "crypto-stake-unlock", receipt: req.StakeId?.Trim());
        tx.AddActor(req.PlayerId, PlayerTransactionActorRole.Recipient);
        tx.AddItemChange("crypto:staked:units", req.Units, ItemOperation.Revoke);
        tx.AddItemChange("crypto:units", req.Units, ItemOperation.Grant);
        tx.MarkApplied();

        db.PlayerTransactions.Add(tx);
        await db.SaveChangesAsync(ct);

        var remainingStaked = await GetStakedUnitsAsync(db, req.PlayerId, ct);
        return Results.Ok(new CryptoStakeResponse(tx.Id, req.PlayerId, req.Units, remainingStaked, tx.Status.ToString()));
    }

    private static async Task<IResult> GetStakingPosition(
        [FromRoute] Guid playerId,
        IAppDb db,
        CancellationToken ct)
    {
        if (playerId == Guid.Empty)
            return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId is required.");

        var staked = await GetStakedUnitsAsync(db, playerId, ct);
        var available = await GetAppliedCryptoUnitsAsync(db, playerId, ct);
        return Results.Ok(new CryptoStakingPositionResponse(playerId, Math.Max(0, available), Math.Max(0, staked), "CRYPTO_UNITS"));
    }

    private static async Task<IResult> ListPendingWithdrawals(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IAppDb db,
        CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100);

        var q = db.PlayerTransactions.AsNoTracking()
            .Include(x => x.Actors)
            .Include(x => x.ItemChanges)
            .Where(x => x.Kind == "crypto-withdraw-request" && x.Status == PlayerTransactionStatus.Pending)
            .OrderBy(x => x.CreatedAtUtc);

        var total = await q.CountAsync(ct);
        var rows = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var items = rows.Select(x =>
        {
            var actor = x.Actors.FirstOrDefault();
            var units = x.ItemChanges
                .Where(i => i.ItemType == "crypto:units")
                .Sum(i => i.Operation == ItemOperation.Grant ? i.Quantity : -i.Quantity);

            return new PendingWithdrawalItem(
                x.Id,
                actor?.PlayerId ?? Guid.Empty,
                Math.Abs(units),
                x.Receipt,
                x.CreatedAtUtc);
        }).ToList();

        return Results.Ok(new PendingWithdrawalsResponse(page, pageSize, total, items));
    }

    private static async Task<IResult> ApproveWithdrawal(
        [FromRoute] Guid transactionId,
        IAppDb db,
        CancellationToken ct)
    {
        if (transactionId == Guid.Empty)
            return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "transactionId is required.");

        var tx = await db.PlayerTransactions
            .FirstOrDefaultAsync(x => x.Id == transactionId && x.Kind == "crypto-withdraw-request", ct);
        if (tx is null)
            return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Withdrawal request not found.");

        if (tx.Status != PlayerTransactionStatus.Pending)
            return ApiResponses.Error(StatusCodes.Status409Conflict, "WITHDRAWAL_NOT_PENDING", "Withdrawal request is not pending.");

        tx.MarkApplied();
        await db.SaveChangesAsync(ct);

        return Results.Ok(new WithdrawalSettlementResponse(tx.Id, tx.Status.ToString(), tx.CompletedAtUtc));
    }

    private static async Task<IResult> RejectWithdrawal(
        [FromRoute] Guid transactionId,
        IAppDb db,
        CancellationToken ct)
    {
        if (transactionId == Guid.Empty)
            return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "transactionId is required.");

        var tx = await db.PlayerTransactions
            .FirstOrDefaultAsync(x => x.Id == transactionId && x.Kind == "crypto-withdraw-request", ct);
        if (tx is null)
            return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Withdrawal request not found.");

        if (tx.Status != PlayerTransactionStatus.Pending)
            return ApiResponses.Error(StatusCodes.Status409Conflict, "WITHDRAWAL_NOT_PENDING", "Withdrawal request is not pending.");

        tx.MarkFailed();
        await db.SaveChangesAsync(ct);

        return Results.Ok(new WithdrawalSettlementResponse(tx.Id, tx.Status.ToString(), tx.CompletedAtUtc));
    }

    private static string NormalizePoolId(string? poolId)
        => string.IsNullOrWhiteSpace(poolId) ? "global" : poolId.Trim().ToLowerInvariant();

    private static async Task<int> GetAppliedCryptoUnitsAsync(IAppDb db, Guid playerId, CancellationToken ct)
    {
        var applied = await db.PlayerTransactions.AsNoTracking()
            .Include(x => x.Actors)
            .Include(x => x.ItemChanges)
            .Where(x =>
                x.Status == PlayerTransactionStatus.Applied &&
                x.Actors.Any(a => a.PlayerId == playerId) &&
                x.ItemChanges.Any(i => i.ItemType == "crypto:units"))
            .ToListAsync(ct);

        return applied.SelectMany(x => x.ItemChanges)
            .Where(i => i.ItemType == "crypto:units")
            .Sum(i => i.Operation == ItemOperation.Grant ? i.Quantity : -i.Quantity);
    }

    private static async Task<int> GetStakedUnitsAsync(IAppDb db, Guid playerId, CancellationToken ct)
    {
        var applied = await db.PlayerTransactions.AsNoTracking()
            .Include(x => x.Actors)
            .Include(x => x.ItemChanges)
            .Where(x =>
                x.Status == PlayerTransactionStatus.Applied &&
                x.Actors.Any(a => a.PlayerId == playerId) &&
                x.ItemChanges.Any(i => i.ItemType == "crypto:staked:units"))
            .ToListAsync(ct);

        return applied.SelectMany(x => x.ItemChanges)
            .Where(i => i.ItemType == "crypto:staked:units")
            .Sum(i => i.Operation == ItemOperation.Grant ? i.Quantity : -i.Quantity);
    }

    private static async Task<int> GetPrizePoolUnitsAsync(IAppDb db, string poolId, CancellationToken ct)
    {
        var applied = await db.PlayerTransactions.AsNoTracking()
            .Include(x => x.ItemChanges)
            .Where(x =>
                x.Status == PlayerTransactionStatus.Applied &&
                x.Receipt == poolId &&
                x.ItemChanges.Any(i => i.ItemType == "crypto:prize-pool:units"))
            .ToListAsync(ct);

        return applied.SelectMany(x => x.ItemChanges)
            .Where(i => i.ItemType == "crypto:prize-pool:units")
            .Sum(i => i.Operation == ItemOperation.Grant ? i.Quantity : -i.Quantity);
    }

    public sealed record LinkWalletRequest(Guid PlayerId, string WalletAddress, string Network = "solana");
    public sealed record LinkWalletResponse(Guid PlayerId, string WalletAddress, string Network, Guid TransactionId, string Status);

    public sealed record CryptoBalanceResponse(Guid PlayerId, int Units, string UnitType);

    public sealed record CryptoHistoryItem(
        Guid TransactionId,
        string Kind,
        int UnitsDelta,
        string Status,
        string? ReceiptRef,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? CompletedAtUtc
    );

    public sealed record CryptoHistoryResponse(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<CryptoHistoryItem> Items
    );

    public sealed record CryptoWithdrawRequest(Guid PlayerId, int Units, string ToWalletAddress, string Network = "solana");
    public sealed record CryptoWithdrawResponse(Guid TransactionId, string Status, int Units, string Network);

    public sealed record CryptoPrizePoolFundRequest(Guid PlayerId, int Units, string? PoolId = "global");
    public sealed record CryptoPrizePoolFundResponse(Guid TransactionId, string PoolId, int UnitsFunded, int PoolUnits, string Status);

    public sealed record CryptoPrizePoolWinner(Guid PlayerId, int Units);
    public sealed record CryptoPrizePoolDistributeRequest(string? PoolId, IReadOnlyList<CryptoPrizePoolWinner> Winners);
    public sealed record CryptoPrizePoolDistributeResponse(string PoolId, int UnitsDistributed, int RemainingPoolUnits, IReadOnlyList<Guid> TransactionIds);

    public sealed record CryptoPrizePoolBalanceResponse(string PoolId, int Units, string UnitType);

    public sealed record CryptoStakeRequest(Guid PlayerId, int Units, string? StakeId = null);
    public sealed record CryptoStakeResponse(Guid TransactionId, Guid PlayerId, int Units, int CurrentStakedUnits, string Status);
    public sealed record CryptoStakingPositionResponse(Guid PlayerId, int AvailableUnits, int StakedUnits, string UnitType);

    public sealed record PendingWithdrawalItem(Guid TransactionId, Guid PlayerId, int Units, string? ToWalletAddress, DateTimeOffset RequestedAtUtc);
    public sealed record PendingWithdrawalsResponse(int Page, int PageSize, int Total, IReadOnlyList<PendingWithdrawalItem> Items);
    public sealed record WithdrawalSettlementResponse(Guid TransactionId, string Status, DateTimeOffset? CompletedAtUtc);
}
