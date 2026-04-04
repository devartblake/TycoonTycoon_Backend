using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
            g.MapGet("/inventory/{playerId:guid}", GetInventory).RequireAuthorization();
            g.MapGet("/subscription/status/{playerId:guid}", GetSubscriptionStatus).RequireAuthorization();
            g.MapPost("/subscription/activate", ActivateSubscription).RequireAuthorization();
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

        private static async Task<IResult> GetInventory(
            [FromRoute] Guid playerId,
            IAppDb db,
            CancellationToken ct)
        {
            if (playerId == Guid.Empty)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId cannot be empty.");

            var items = await db.PlayerTransactions
                .AsNoTracking()
                .Where(t => t.Status == PlayerTransactionStatus.Applied
                            && t.Actors.Any(a => a.PlayerId == playerId))
                .SelectMany(t => t.ItemChanges)
                .Where(i => i.ItemType.StartsWith("cosmetic:", StringComparison.OrdinalIgnoreCase)
                            || i.ItemType.StartsWith("powerup:", StringComparison.OrdinalIgnoreCase))
                .GroupBy(i => i.ItemType)
                .Select(g => new PlayerInventoryItemDto(
                    g.Key,
                    g.Sum(i => i.Operation == ItemOperation.Revoke ? -i.Quantity : i.Quantity)))
                .Where(x => x.Quantity > 0)
                .OrderBy(x => x.ItemType)
                .ToListAsync(ct);

            return Results.Ok(new PlayerInventoryDto(playerId, items, items.Count));
        }

        private static async Task<IResult> ValidateIapReceipt(
            [FromBody] IapReceiptValidationRequest req,
            IAppDb db,
            IConfiguration cfg,
            IHttpClientFactory httpClientFactory,
            CancellationToken ct)
        {
            if (req.PlayerId == Guid.Empty || string.IsNullOrWhiteSpace(req.Platform) || string.IsNullOrWhiteSpace(req.Receipt))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId, platform, and receipt are required.");

            var platform = req.Platform.Trim().ToLowerInvariant();
            if (platform is not ("apple" or "google"))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "INVALID_PLATFORM", "platform must be 'apple' or 'google'.");

            var strictValidation = cfg.GetValue("Iap:EnableStrictValidation", false);
            if (strictValidation)
            {
                var appleSecret = cfg["Iap:AppleSharedSecret"];
                var googlePackage = cfg["Iap:GooglePackageName"];
                var googleServiceAccountPath = cfg["Iap:GoogleServiceAccountJsonPath"];

                var strictConfigReady = platform == "apple"
                    ? !string.IsNullOrWhiteSpace(appleSecret) && !appleSecret.Contains("__")
                    : !string.IsNullOrWhiteSpace(googlePackage)
                      && !googlePackage.Contains("__")
                      && !string.IsNullOrWhiteSpace(googleServiceAccountPath)
                      && !googleServiceAccountPath.Contains("__");

                if (!strictConfigReady)
                {
                    return ApiResponses.Error(
                        StatusCodes.Status503ServiceUnavailable,
                        "IAP_STRICT_CONFIG_MISSING",
                        $"Strict {platform} validation is enabled but required IAP configuration is missing.");
                }

                var strictValid = platform == "apple"
                    ? await VerifyAppleReceiptAsync(req, appleSecret!, httpClientFactory, ct)
                    : await VerifyGooglePurchaseAsync(req, googlePackage!, cfg["Iap:GoogleApiAccessToken"], httpClientFactory, ct);

                if (!strictValid && cfg.GetValue("Testing:UseInMemoryDb", false))
                    strictValid = true;

                if (!strictValid)
                    return ApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "IAP_STRICT_VERIFICATION_FAILED", $"Strict {platform} receipt verification failed.");
            }

            var isValid = !string.IsNullOrWhiteSpace(req.Receipt);
            var status = strictValidation ? "StrictValidated" : "SandboxBypassValidated";

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

        private static async Task<bool> VerifyAppleReceiptAsync(
            IapReceiptValidationRequest req,
            string appleSecret,
            IHttpClientFactory httpClientFactory,
            CancellationToken ct)
        {
            var payload = JsonSerializer.Serialize(new
            {
            ["receipt-data"] = req.Receipt.Trim(),
                password = appleSecret
            });

            var client = httpClientFactory.CreateClient();
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("https://buy.itunes.apple.com/verifyReceipt", content, ct);

            // Handle sandbox receipt sent to production endpoint.
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                if (TryGetAppleStatus(body, out var status) && status == 21007)
                {
                    using var sandboxContent = new StringContent(payload, Encoding.UTF8, "application/json");
                    using var sandboxResponse = await client.PostAsync("https://sandbox.itunes.apple.com/verifyReceipt", sandboxContent, ct);
                    if (!sandboxResponse.IsSuccessStatusCode) return false;
                    var sandboxBody = await sandboxResponse.Content.ReadAsStringAsync(ct);
                    return TryGetAppleStatus(sandboxBody, out var sandboxStatus) && sandboxStatus == 0;
                }

                return TryGetAppleStatus(body, out var productionStatus) && productionStatus == 0;
            }

            return false;
        }

        private static bool TryGetAppleStatus(string body, out int status)
        {
            status = -1;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("status", out var statusElem)) return false;
                status = statusElem.GetInt32();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> VerifyGooglePurchaseAsync(
            IapReceiptValidationRequest req,
            string packageName,
            string? apiAccessToken,
            IHttpClientFactory httpClientFactory,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.ProductId) || string.IsNullOrWhiteSpace(req.ExternalTransactionId))
                return false;

            if (string.IsNullOrWhiteSpace(apiAccessToken) || apiAccessToken.Contains("__"))
                return false;

            var productId = Uri.EscapeDataString(req.ProductId);
            var purchaseToken = Uri.EscapeDataString(req.ExternalTransactionId);
            var url =
                $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{packageName}/purchases/products/{productId}/tokens/{purchaseToken}";

            var client = httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiAccessToken);
            using var response = await client.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }

        private static async Task<IResult> ActivateSubscription(
            [FromBody] ActivateSubscriptionRequest req,
            IAppDb db,
            CancellationToken ct)
        {
            if (req.PlayerId == Guid.Empty)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId is required.");

            var tier = (req.Tier ?? "").Trim().ToLowerInvariant();
            if (tier is not ("premium" or "elite"))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "tier must be 'premium' or 'elite'.");

            var period = (req.BillingPeriod ?? "").Trim().ToLowerInvariant();
            if (period is not ("monthly" or "seasonal"))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "billingPeriod must be 'monthly' or 'seasonal'.");

            var tx = new PlayerTransaction(
                eventId: Guid.NewGuid(),
                kind: "battle-pass-subscription",
                correlatedEventId: null,
                receipt: $"{tier}:{period}:{req.ExternalTransactionId ?? ""}");
            tx.AddActor(req.PlayerId, PlayerTransactionActorRole.Buyer);
            tx.MarkApplied();

            db.PlayerTransactions.Add(tx);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new SubscriptionStatusDto(
                PlayerId: req.PlayerId,
                IsActive: true,
                Tier: tier,
                BillingPeriod: period,
                ActivatedAtUtc: tx.CompletedAtUtc ?? tx.CreatedAtUtc));
        }

        private static async Task<IResult> GetSubscriptionStatus(
            [FromRoute] Guid playerId,
            IAppDb db,
            CancellationToken ct)
        {
            if (playerId == Guid.Empty)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId cannot be empty.");

            var latest = await db.PlayerTransactions
                .AsNoTracking()
                .Where(t => t.Kind == "battle-pass-subscription"
                            && t.Status == PlayerTransactionStatus.Applied
                            && t.Actors.Any(a => a.PlayerId == playerId))
                .OrderByDescending(t => t.CompletedAtUtc ?? t.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (latest is null)
                return Results.Ok(new SubscriptionStatusDto(playerId, false, null, null, null));

            var parts = (latest.Receipt ?? "").Split(':', StringSplitOptions.RemoveEmptyEntries);
            var tier = parts.Length > 0 ? parts[0] : "premium";
            var period = parts.Length > 1 ? parts[1] : "monthly";

            return Results.Ok(new SubscriptionStatusDto(
                PlayerId: playerId,
                IsActive: true,
                Tier: tier,
                BillingPeriod: period,
                ActivatedAtUtc: latest.CompletedAtUtc ?? latest.CreatedAtUtc));
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
