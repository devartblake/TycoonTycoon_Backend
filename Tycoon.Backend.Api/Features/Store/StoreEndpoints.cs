using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.PlayerTransactions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Store
{
    public static class StoreEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/store").WithTags("Store").WithOpenApi();

            g.MapGet("/catalog", GetCatalog);
            g.MapGet("/catalog/{sku}", GetItem);
            g.MapPost("/purchase", Purchase).RequireAuthorization();
            g.MapPost("/iap/validate", ValidateIapReceipt).RequireAuthorization();
        }

        private static async Task<IResult> GetCatalog(
            [FromQuery] string? itemType,
            IAppDb db,
            CancellationToken ct)
        {
            var query = db.StoreItems
                .AsNoTracking()
                .Where(i => i.IsActive);

            if (!string.IsNullOrWhiteSpace(itemType))
                query = query.Where(i => i.ItemType == itemType);

            var items = await query
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Name)
                .Select(i => new StoreItemDto(
                    i.Id, i.Sku, i.Name, i.Description, i.ItemType,
                    i.PriceCoins, i.PriceDiamonds, i.GrantQuantity,
                    i.MaxPerPlayer, i.MediaKey, i.SortOrder))
                .ToListAsync(ct);

            return Results.Ok(new StoreCatalogDto(items, items.Count));
        }

        private static async Task<IResult> GetItem(
            [FromRoute] string sku,
            IAppDb db,
            CancellationToken ct)
        {
            var item = await db.StoreItems
                .AsNoTracking()
                .Where(i => i.Sku == sku && i.IsActive)
                .Select(i => new StoreItemDto(
                    i.Id, i.Sku, i.Name, i.Description, i.ItemType,
                    i.PriceCoins, i.PriceDiamonds, i.GrantQuantity,
                    i.MaxPerPlayer, i.MediaKey, i.SortOrder))
                .FirstOrDefaultAsync(ct);

            return item is null
                ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Store item not found.")
                : Results.Ok(item);
        }

        private static async Task<IResult> Purchase(
            [FromBody] StorePurchaseRequest req,
            IAppDb db,
            PlayerTransactionService txnService,
            CancellationToken ct)
        {
            // Validate currency parameter
            var currency = req.Currency?.ToLowerInvariant();
            if (currency is not ("coins" or "diamonds"))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_CURRENCY",
                    "Currency must be 'coins' or 'diamonds'.");

            if (req.Quantity <= 0)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_QUANTITY",
                    "Quantity must be at least 1.");

            // Look up the catalog item
            var storeItem = await db.StoreItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Sku == req.Sku && i.IsActive, ct);

            if (storeItem is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Store item not found or not available.");

            // Validate price exists for chosen currency
            var unitPrice = currency == "coins" ? storeItem.PriceCoins : storeItem.PriceDiamonds;
            if (unitPrice <= 0)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "CURRENCY_NOT_ACCEPTED",
                    $"This item cannot be purchased with {currency}.");

            var totalPrice = unitPrice * req.Quantity;
            var totalGranted = storeItem.GrantQuantity * req.Quantity;

            // Check per-player purchase limit if set
            if (storeItem.MaxPerPlayer > 0)
            {
                var previousPurchases = await db.PlayerTransactions
                    .AsNoTracking()
                    .CountAsync(t =>
                        t.Kind == "store-purchase" &&
                        t.Status == PlayerTransactionStatus.Applied &&
                        t.Actors.Any(a => a.PlayerId == req.PlayerId) &&
                        t.ItemChanges.Any(i => i.ItemType == storeItem.Sku),
                        ct);

                if (previousPurchases + req.Quantity > storeItem.MaxPerPlayer)
                    return ApiResponses.Error(StatusCodes.Status409Conflict, "PURCHASE_LIMIT",
                        $"Purchase limit of {storeItem.MaxPerPlayer} reached for this item.");
            }

            // Build the player transaction request
            var eventId = Guid.NewGuid();
            var currencyType = currency == "coins" ? CurrencyType.Coins : CurrencyType.Diamonds;

            var ptxnReq = new CreatePlayerTransactionRequest(
                EventId: eventId,
                Kind: "store-purchase",
                Actors: new[] { new PlayerTransactionActorDto(req.PlayerId, "buyer") },
                CurrencyChanges: new[]
                {
                    new PlayerTransactionCurrencyDto(
                        req.PlayerId,
                        new[] { new EconomyLineDto(currencyType, -totalPrice) })
                },
                ItemChanges: new[]
                {
                    new PlayerTransactionItemDto(storeItem.Sku, totalGranted, "grant")
                },
                Note: $"Store purchase: {req.Quantity}x {storeItem.Name}"
            );

            var result = await txnService.ExecuteAsync(ptxnReq, ct);

            // Map to store-specific response
            var balanceResult = result.EconomyResults.FirstOrDefault();

            return Results.Ok(new StorePurchaseResultDto(
                Status: result.Status,
                TransactionId: result.Status == "Applied" ? result.PlayerTransactionId : null,
                BalanceXp: balanceResult?.BalanceXp ?? 0,
                BalanceCoins: balanceResult?.BalanceCoins ?? 0,
                BalanceDiamonds: balanceResult?.BalanceDiamonds ?? 0,
                ErrorMessage: result.Status != "Applied" ? $"Purchase failed: {result.Status}" : null));
        }

        private static async Task<IResult> ValidateIapReceipt(
            [FromBody] IapReceiptValidationRequest req,
            IAppDb db,
            IConfiguration cfg,
            CancellationToken ct)
        {
            if (req.PlayerId == Guid.Empty || string.IsNullOrWhiteSpace(req.Platform) || string.IsNullOrWhiteSpace(req.Receipt))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId, platform, and receipt are required.");

            var platform = req.Platform.Trim().ToLowerInvariant();
            if (platform is not ("apple" or "google"))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_PLATFORM", "platform must be 'apple' or 'google'.");

            var strictValidation = cfg.GetValue("Iap:EnableStrictValidation", false);
            var isValid = !string.IsNullOrWhiteSpace(req.Receipt);
            var status = strictValidation ? "Validated" : "SandboxBypassValidated";

            var tx = new PlayerTransaction(
                eventId: Guid.NewGuid(),
                kind: "iap-receipt-validation",
                correlatedEventId: null,
                receipt: req.Receipt.Trim()
            );

            tx.AddActor(req.PlayerId, PlayerTransactionActorRole.Buyer);
            if (isValid)
                tx.MarkApplied();
            else
                tx.MarkFailed();

            db.PlayerTransactions.Add(tx);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new IapReceiptValidationResponse(
                Valid: isValid,
                Platform: platform,
                Status: status,
                TransactionId: tx.Id,
                ProductId: req.ProductId,
                ExternalTransactionId: req.ExternalTransactionId
            ));
        }

        public sealed record IapReceiptValidationRequest(
            Guid PlayerId,
            string Platform,
            string Receipt,
            string? ProductId = null,
            string? ExternalTransactionId = null
        );

        public sealed record IapReceiptValidationResponse(
            bool Valid,
            string Platform,
            string Status,
            Guid TransactionId,
            string? ProductId,
            string? ExternalTransactionId
        );
    }
}
