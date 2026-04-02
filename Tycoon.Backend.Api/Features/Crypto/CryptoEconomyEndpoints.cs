using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Api.Features.Crypto;

public static class CryptoEconomyEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/crypto").WithTags("Crypto Economy").WithOpenApi();

        g.MapPost("/link-wallet", LinkWallet).RequireAuthorization();
        g.MapGet("/balance/{playerId:guid}", GetBalance).RequireAuthorization();
        g.MapGet("/history/{playerId:guid}", GetHistory).RequireAuthorization();
        g.MapPost("/withdraw", RequestWithdrawal).RequireAuthorization();
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
                x.Kind.StartsWith("crypto-", StringComparison.Ordinal) &&
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
}
