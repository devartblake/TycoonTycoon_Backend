using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Api.Payments.PayPal;
using Tycoon.Backend.Api.Payments.Stripe;
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
            g.MapPost("/subscription/checkout/session", CreateStripeSubscriptionCheckoutSession).RequireAuthorization();
            g.MapPost("/subscription/portal/session", CreateStripeBillingPortalSession).RequireAuthorization();
            g.MapPost("/subscription/paypal/create", CreatePayPalSubscription).RequireAuthorization();
            g.MapPost("/subscription/paypal/cancel", CancelPayPalSubscription).RequireAuthorization();
            g.MapPost("/purchase", Purchase).RequireAuthorization();
            g.MapPost("/payments/checkout/session", CreateStripeCheckoutSession).RequireAuthorization();
            g.MapPost("/payments/paypal/order", CreatePayPalOrder).RequireAuthorization();
            g.MapPost("/payments/paypal/capture", CapturePayPalOrder).RequireAuthorization();
            g.MapPost("/payments/webhook", HandleStripeWebhook);
            g.MapPost("/payments/paypal/webhook", HandlePayPalWebhook);
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

        private static async Task<IResult> CreateStripeCheckoutSession(
            [FromBody] CreateStripeCheckoutSessionRequest req,
            HttpContext httpContext,
            IAppDb db,
            IStripePaymentGateway stripeGateway,
            IOptions<StripeOptions> stripeOptionsAccessor,
            CancellationToken ct)
        {
            if (req.PlayerId == Guid.Empty)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId is required.");

            if (req.Quantity <= 0)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "quantity must be at least 1.");

            if (!TryGetAuthenticatedPlayerId(httpContext.User, out var authenticatedPlayerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "A valid bearer token is required.");

            if (authenticatedPlayerId != req.PlayerId)
                return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only create a Stripe checkout session for your own player account.");

            var storeItem = await db.StoreItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Sku == req.Sku && i.IsActive, ct);

            if (storeItem is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Store item not found or not available.");

            if (storeItem.MaxPerPlayer > 0)
            {
                var previousPurchases = await db.PlayerTransactions
                    .AsNoTracking()
                    .CountAsync(t =>
                        t.Kind == "stripe-checkout-payment"
                        && t.Status == PlayerTransactionStatus.Applied
                        && t.Actors.Any(a => a.PlayerId == req.PlayerId)
                        && t.ItemChanges.Any(i => i.ItemType == storeItem.Sku),
                        ct);

                if (previousPurchases + req.Quantity > storeItem.MaxPerPlayer)
                    return ApiResponses.Error(StatusCodes.Status409Conflict, "PURCHASE_LIMIT",
                        $"Purchase limit of {storeItem.MaxPerPlayer} reached for this item.");
            }

            var stripeOptions = stripeOptionsAccessor.Value;
            var catalogItem = stripeOptions.Catalog.FirstOrDefault(i =>
                string.Equals(i.Sku, storeItem.Sku, StringComparison.OrdinalIgnoreCase));

            if (catalogItem is null || catalogItem.UnitAmount <= 0)
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "STRIPE_PRICE_NOT_CONFIGURED",
                    $"Stripe pricing is not configured for SKU '{storeItem.Sku}'.");

            var successUrl = req.SuccessUrl ?? stripeOptions.SuccessUrl;
            var cancelUrl = req.CancelUrl ?? stripeOptions.CancelUrl;

            if (!IsValidAbsoluteHttpUrl(successUrl) || !IsValidAbsoluteHttpUrl(cancelUrl))
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "STRIPE_REDIRECT_URL_NOT_CONFIGURED",
                    "Stripe success and cancel URLs must be configured as absolute HTTP or HTTPS URLs.");

            var playerEmail = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == req.PlayerId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(ct);

            try
            {
                var currency = (catalogItem.Currency ?? stripeOptions.DefaultCurrency).Trim().ToLowerInvariant();
                var unitAmount = catalogItem.UnitAmount;
                var totalAmount = unitAmount * req.Quantity;

                var checkoutSession = await stripeGateway.CreateCheckoutSessionAsync(
                    new StripeCheckoutSessionCreateRequest(
                        req.PlayerId,
                        playerEmail,
                        storeItem.Sku,
                        catalogItem.ProductName ?? storeItem.Name,
                        catalogItem.ProductDescription ?? storeItem.Description,
                        req.Quantity,
                        unitAmount,
                        currency,
                        successUrl!,
                        cancelUrl!,
                        new Dictionary<string, string>
                        {
                            ["player_id"] = req.PlayerId.ToString(),
                            ["sku"] = storeItem.Sku,
                            ["quantity"] = req.Quantity.ToString(CultureInfo.InvariantCulture)
                        }),
                    ct);

                return Results.Ok(new CreateStripeCheckoutSessionResponse(
                    checkoutSession.SessionId,
                    checkoutSession.CheckoutUrl,
                    currency,
                    unitAmount,
                    totalAmount,
                    storeItem.Sku,
                    req.Quantity,
                    stripeOptions.PublishableKey));
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "STRIPE_NOT_READY", ex.Message);
            }
        }

        private static async Task<IResult> CreatePayPalOrder(
            [FromBody] CreatePayPalOrderRequest req,
            HttpContext httpContext,
            IAppDb db,
            IPayPalPaymentGateway payPalGateway,
            IOptions<PayPalOptions> payPalOptionsAccessor,
            CancellationToken ct)
        {
            if (req.PlayerId == Guid.Empty)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId is required.");

            if (req.Quantity <= 0)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "quantity must be at least 1.");

            if (!TryGetAuthenticatedPlayerId(httpContext.User, out var authenticatedPlayerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "A valid bearer token is required.");

            if (authenticatedPlayerId != req.PlayerId)
                return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only create a PayPal order for your own player account.");

            var storeItem = await db.StoreItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Sku == req.Sku && i.IsActive, ct);

            if (storeItem is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Store item not found or not available.");

            var payPalOptions = payPalOptionsAccessor.Value;
            var catalogItem = payPalOptions.Catalog.FirstOrDefault(i =>
                string.Equals(i.Sku, storeItem.Sku, StringComparison.OrdinalIgnoreCase));

            if (catalogItem is null || catalogItem.UnitAmount <= 0)
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "PAYPAL_PRICE_NOT_CONFIGURED",
                    $"PayPal pricing is not configured for SKU '{storeItem.Sku}'.");

            var returnUrl = req.ReturnUrl ?? payPalOptions.ReturnUrl;
            var cancelUrl = req.CancelUrl ?? payPalOptions.CancelUrl;
            if (!IsValidAbsoluteHttpUrl(returnUrl) || !IsValidAbsoluteHttpUrl(cancelUrl))
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "PAYPAL_REDIRECT_URL_NOT_CONFIGURED",
                    "PayPal return and cancel URLs must be configured as absolute HTTP or HTTPS URLs.");

            try
            {
                var unitAmount = catalogItem.UnitAmount;
                var totalAmount = unitAmount * req.Quantity;
                var order = await payPalGateway.CreateOrderAsync(
                    new PayPalCreateOrderRequest(
                        req.PlayerId,
                        storeItem.Sku,
                        storeItem.Name,
                        storeItem.Description,
                        req.Quantity,
                        unitAmount,
                        catalogItem.Currency,
                        returnUrl!,
                        cancelUrl!),
                    ct);

                return Results.Ok(new CreatePayPalOrderResponse(
                    order.OrderId,
                    order.Status,
                    order.ApproveUrl,
                    catalogItem.Currency,
                    unitAmount,
                    totalAmount,
                    storeItem.Sku,
                    req.Quantity,
                    payPalOptions.ClientId));
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "PAYPAL_NOT_READY", ex.Message);
            }
        }

        private static async Task<IResult> CapturePayPalOrder(
            [FromBody] CapturePayPalOrderRequest req,
            HttpContext httpContext,
            IAppDb db,
            IPayPalPaymentGateway payPalGateway,
            CancellationToken ct)
        {
            if (req.PlayerId == Guid.Empty || string.IsNullOrWhiteSpace(req.OrderId))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId and orderId are required.");

            if (!TryGetAuthenticatedPlayerId(httpContext.User, out var authenticatedPlayerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "A valid bearer token is required.");

            if (authenticatedPlayerId != req.PlayerId)
                return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only capture your own PayPal order.");

            try
            {
                var capture = await payPalGateway.CaptureOrderAsync(req.OrderId, ct);
                var metadata = ParsePayPalCustomId(capture.CustomId);
                if (metadata is null || metadata.PlayerId != req.PlayerId)
                    return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "PayPal order ownership could not be confirmed.");

                var storeItem = await db.StoreItems.FirstOrDefaultAsync(i => i.Sku == metadata.Sku, ct);
                if (storeItem is null)
                    return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", $"Store item '{metadata.Sku}' was not found.");

                var eventId = CreateDeterministicGuid($"paypal-order:{capture.OrderId}");
                var existing = await db.PlayerTransactions.AsNoTracking().AnyAsync(t => t.EventId == eventId, ct);
                if (existing)
                    return Results.Ok(new CapturePayPalOrderResponse(capture.OrderId, capture.Status, capture.CaptureId, null));

                var tx = new PlayerTransaction(
                    eventId: eventId,
                    kind: "paypal-order-payment",
                    correlatedEventId: null,
                    receipt: capture.OrderId);
                tx.AddActor(req.PlayerId, PlayerTransactionActorRole.Buyer);
                tx.AddItemChange(storeItem.Sku, storeItem.GrantQuantity * metadata.Quantity, ItemOperation.Grant);
                tx.MarkApplied();

                db.PlayerTransactions.Add(tx);
                await db.SaveChangesAsync(ct);

                return Results.Ok(new CapturePayPalOrderResponse(capture.OrderId, capture.Status, capture.CaptureId, tx.Id));
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "PAYPAL_NOT_READY", ex.Message);
            }
        }

        private static async Task<IResult> HandlePayPalWebhook(
            HttpRequest request,
            IAppDb db,
            IPayPalPaymentGateway payPalGateway,
            IOptions<PayPalOptions> payPalOptionsAccessor,
            CancellationToken ct)
        {
            string payload;
            using (var reader = new StreamReader(request.Body, Encoding.UTF8))
            {
                payload = await reader.ReadToEndAsync(ct);
            }

            var webhookId = payPalOptionsAccessor.Value.WebhookId;
            if (string.IsNullOrWhiteSpace(webhookId))
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "PAYPAL_NOT_READY", "PayPal WebhookId must be configured.");

            if (!request.Headers.TryGetValue("PAYPAL-TRANSMISSION-ID", out var transmissionId)
                || !request.Headers.TryGetValue("PAYPAL-TRANSMISSION-TIME", out var transmissionTime)
                || !request.Headers.TryGetValue("PAYPAL-TRANSMISSION-SIG", out var transmissionSig)
                || !request.Headers.TryGetValue("PAYPAL-CERT-URL", out var certUrl)
                || !request.Headers.TryGetValue("PAYPAL-AUTH-ALGO", out var authAlgo))
            {
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "PAYPAL_WEBHOOK_INVALID", "Required PayPal webhook headers were missing.");
            }

            bool verified;
            try
            {
                verified = await payPalGateway.VerifyWebhookAsync(
                    new PayPalWebhookVerificationRequest(
                        transmissionId.ToString(),
                        transmissionTime.ToString(),
                        transmissionSig.ToString(),
                        certUrl.ToString(),
                        authAlgo.ToString(),
                        webhookId,
                        payload),
                    ct);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "PAYPAL_NOT_READY", ex.Message);
            }

            if (!verified)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "PAYPAL_WEBHOOK_INVALID", "PayPal webhook signature verification failed.");

            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            var eventType = root.TryGetProperty("event_type", out var eventTypeElement)
                ? eventTypeElement.GetString()
                : null;
            var eventId = root.TryGetProperty("id", out var eventIdElement)
                ? eventIdElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(eventId))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "PAYPAL_WEBHOOK_INVALID", "PayPal webhook payload was missing event_type or id.");

            if (!root.TryGetProperty("resource", out var resource))
                return Results.Ok(new { received = true, ignored = true, eventType });

            if (string.Equals(eventType, "PAYMENT.CAPTURE.COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                var orderId = resource.TryGetProperty("supplementary_data", out var supplementary)
                    && supplementary.TryGetProperty("related_ids", out var relatedIds)
                    && relatedIds.TryGetProperty("order_id", out var orderIdElement)
                    ? orderIdElement.GetString()
                    : null;

                var customId = resource.TryGetProperty("custom_id", out var customIdElement)
                    ? customIdElement.GetString()
                    : null;

                var metadata = ParsePayPalCustomId(customId);
                if (string.IsNullOrWhiteSpace(orderId) || metadata is null)
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "PAYPAL_WEBHOOK_INVALID", "PayPal capture webhook was missing ownership metadata.");

                var webhookEventId = CreateDeterministicGuid($"paypal-webhook:{eventId}");
                var existingWebhook = await db.PlayerTransactions.AsNoTracking().AnyAsync(t => t.EventId == webhookEventId, ct);
                if (existingWebhook)
                    return Results.Ok(new { received = true, duplicate = true, eventType });

                var storeItem = await db.StoreItems.FirstOrDefaultAsync(i => i.Sku == metadata.Sku, ct);
                if (storeItem is null)
                    return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", $"Store item '{metadata.Sku}' was not found.");

                var tx = new PlayerTransaction(
                    eventId: webhookEventId,
                    kind: "paypal-order-payment",
                    correlatedEventId: null,
                    receipt: orderId);
                tx.AddActor(metadata.PlayerId, PlayerTransactionActorRole.Buyer);
                tx.AddItemChange(storeItem.Sku, storeItem.GrantQuantity * metadata.Quantity, ItemOperation.Grant);
                tx.MarkApplied();

                db.PlayerTransactions.Add(tx);
                await db.SaveChangesAsync(ct);

                return Results.Ok(new { received = true, applied = true, eventType, orderId });
            }

            if (eventType.StartsWith("BILLING.SUBSCRIPTION.", StringComparison.OrdinalIgnoreCase))
            {
                var subscriptionId = resource.TryGetProperty("id", out var subIdElement) ? subIdElement.GetString() : null;
                var customId = resource.TryGetProperty("custom_id", out var customIdElement) ? customIdElement.GetString() : null;
                var status = resource.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;
                var metadata = ParsePayPalSubscriptionCustomId(customId);

                if (string.IsNullOrWhiteSpace(subscriptionId) || metadata is null)
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "PAYPAL_WEBHOOK_INVALID", "PayPal subscription webhook was missing ownership metadata.");

                var webhookEventId = CreateDeterministicGuid($"paypal-webhook:{eventId}");
                var existingWebhook = await db.PlayerTransactions.AsNoTracking().AnyAsync(t => t.EventId == webhookEventId, ct);
                if (existingWebhook)
                    return Results.Ok(new { received = true, duplicate = true, eventType, subscriptionId });

                DateTimeOffset? nextBillingTimeUtc = resource.TryGetProperty("billing_info", out var billingInfo)
                    && billingInfo.TryGetProperty("next_billing_time", out var nextBilling)
                    && DateTimeOffset.TryParse(nextBilling.GetString(), out var parsedNextBilling)
                    ? parsedNextBilling
                    : null;

                var cancelAtPeriodEnd =
                    string.Equals(status, "CANCELLED", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(status, "SUSPENDED", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(status, "EXPIRED", StringComparison.OrdinalIgnoreCase);

                var tx = new PlayerTransaction(
                    eventId: webhookEventId,
                    kind: string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase)
                        ? "paypal-subscription-activated"
                        : "paypal-subscription-status",
                    correlatedEventId: null,
                    receipt: SerializeSubscriptionReceipt(
                        provider: "paypal",
                        tier: metadata.Tier,
                        billingPeriod: metadata.BillingPeriod,
                        subscriptionId: subscriptionId,
                        customerId: null,
                        status: status,
                        currentPeriodEndUtc: nextBillingTimeUtc,
                        cancelAtPeriodEnd: cancelAtPeriodEnd));
                tx.AddActor(metadata.PlayerId, PlayerTransactionActorRole.Buyer);
                tx.MarkApplied();

                db.PlayerTransactions.Add(tx);
                await db.SaveChangesAsync(ct);

                return Results.Ok(new { received = true, applied = true, eventType, subscriptionId, status });
            }

            return Results.Ok(new { received = true, ignored = true, eventType });
        }

        private static async Task<IResult> HandleStripeWebhook(
            HttpRequest request,
            IAppDb db,
            IStripePaymentGateway stripeGateway,
            CancellationToken ct)
        {
            string payload;
            using (var reader = new StreamReader(request.Body, Encoding.UTF8))
            {
                payload = await reader.ReadToEndAsync(ct);
            }

            StripeWebhookEvent webhookEvent;
            try
            {
                webhookEvent = stripeGateway.ParseWebhook(payload, request.Headers["Stripe-Signature"]);
            }
            catch (global::Stripe.StripeException ex)
            {
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "STRIPE_WEBHOOK_INVALID", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "STRIPE_NOT_READY", ex.Message);
            }

            if (string.Equals(webhookEvent.EventType, global::Stripe.Events.CustomerSubscriptionUpdated, StringComparison.Ordinal)
                || string.Equals(webhookEvent.EventType, global::Stripe.Events.CustomerSubscriptionDeleted, StringComparison.Ordinal))
            {
                var subEvent = webhookEvent.SubscriptionChanged;
                if (subEvent is null)
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "STRIPE_WEBHOOK_INVALID", "Subscription payload was missing.");

                if (!subEvent.Metadata.TryGetValue("player_id", out var subscriptionPlayerIdRaw) || !Guid.TryParse(subscriptionPlayerIdRaw, out var subscriptionPlayerId))
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "STRIPE_WEBHOOK_INVALID", "Stripe subscription metadata is missing a valid player_id.");

                var subscriptionEventId = CreateDeterministicGuid(webhookEvent.EventId);
                var existingSubscriptionEvent = await db.PlayerTransactions.AsNoTracking().AnyAsync(t => t.EventId == subscriptionEventId, ct);
                if (existingSubscriptionEvent)
                    return Results.Ok(new { received = true, duplicate = true, eventType = webhookEvent.EventType });

                var tier = subEvent.Metadata.TryGetValue("tier", out var tierValue) ? tierValue : "premium";
                var billingPeriod = subEvent.Metadata.TryGetValue("billing_period", out var billingValue) ? billingValue : "monthly";
                var receipt = SerializeSubscriptionReceipt(
                    provider: "stripe",
                    tier: tier,
                    billingPeriod: billingPeriod,
                    subscriptionId: subEvent.SubscriptionId,
                    customerId: subEvent.CustomerId,
                    status: subEvent.Status,
                    currentPeriodEndUtc: subEvent.CurrentPeriodEndUtc,
                    cancelAtPeriodEnd: subEvent.CancelAtPeriodEnd);

                var subscriptionTx = new PlayerTransaction(
                    eventId: subscriptionEventId,
                    kind: "stripe-subscription-status",
                    correlatedEventId: null,
                    receipt: receipt);
                subscriptionTx.AddActor(subscriptionPlayerId, PlayerTransactionActorRole.Buyer);
                subscriptionTx.MarkApplied();

                db.PlayerTransactions.Add(subscriptionTx);
                await db.SaveChangesAsync(ct);

                return Results.Ok(new { received = true, applied = true, eventType = webhookEvent.EventType });
            }

            if (!string.Equals(webhookEvent.EventType, global::Stripe.Events.CheckoutSessionCompleted, StringComparison.Ordinal))
                return Results.Ok(new { received = true, ignored = true, eventType = webhookEvent.EventType });

            var completed = webhookEvent.CheckoutCompleted;
            if (completed is null)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "STRIPE_WEBHOOK_INVALID", "Checkout session payload was missing.");

            if (string.Equals(completed.Mode, "subscription", StringComparison.OrdinalIgnoreCase))
            {
                if (!completed.Metadata.TryGetValue("player_id", out var subscriptionCheckoutPlayerIdRaw) || !Guid.TryParse(subscriptionCheckoutPlayerIdRaw, out var subscriptionCheckoutPlayerId))
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "STRIPE_WEBHOOK_INVALID", "Stripe checkout metadata is missing a valid player_id.");

                var subscriptionCheckoutEventId = CreateDeterministicGuid(completed.SessionId);
                var existingSubscriptionCheckout = await db.PlayerTransactions
                    .AsNoTracking()
                    .AnyAsync(t => t.EventId == subscriptionCheckoutEventId, ct);

                if (existingSubscriptionCheckout)
                    return Results.Ok(new { received = true, duplicate = true, sessionId = completed.SessionId });

                var tier = completed.Metadata.TryGetValue("tier", out var tierValue) ? tierValue : "premium";
                var billingPeriod = completed.Metadata.TryGetValue("billing_period", out var billingValue) ? billingValue : "monthly";
                var receipt = SerializeSubscriptionReceipt(
                    provider: "stripe",
                    tier: tier,
                    billingPeriod: billingPeriod,
                    subscriptionId: completed.SubscriptionId,
                    customerId: completed.CustomerId,
                    status: "active",
                    currentPeriodEndUtc: null,
                    cancelAtPeriodEnd: false);

                var subscriptionCheckoutTx = new PlayerTransaction(
                    eventId: subscriptionCheckoutEventId,
                    kind: "stripe-subscription-activated",
                    correlatedEventId: null,
                    receipt: receipt);

                subscriptionCheckoutTx.AddActor(subscriptionCheckoutPlayerId, PlayerTransactionActorRole.Buyer);
                subscriptionCheckoutTx.MarkApplied();

                db.PlayerTransactions.Add(subscriptionCheckoutTx);
                await db.SaveChangesAsync(ct);

                return Results.Ok(new
                {
                    received = true,
                    applied = true,
                    mode = "subscription",
                    sessionId = completed.SessionId,
                    subscriptionId = completed.SubscriptionId
                });
            }

            if (!string.Equals(completed.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                return Results.Ok(new { received = true, ignored = true, reason = "payment_not_paid" });

            if (!completed.Metadata.TryGetValue("player_id", out var playerIdRaw) || !Guid.TryParse(playerIdRaw, out var playerId))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "STRIPE_WEBHOOK_INVALID", "Stripe checkout metadata is missing a valid player_id.");

            if (!completed.Metadata.TryGetValue("sku", out var sku) || string.IsNullOrWhiteSpace(sku))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "STRIPE_WEBHOOK_INVALID", "Stripe checkout metadata is missing sku.");

            if (!completed.Metadata.TryGetValue("quantity", out var quantityRaw) || !int.TryParse(quantityRaw, out var quantity) || quantity <= 0)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "STRIPE_WEBHOOK_INVALID", "Stripe checkout metadata is missing a valid quantity.");

            var storeItem = await db.StoreItems
                .FirstOrDefaultAsync(i => i.Sku == sku, ct);

            if (storeItem is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", $"Store item '{sku}' was not found.");

            var eventId = CreateDeterministicGuid(completed.SessionId);
            var existing = await db.PlayerTransactions
                .AsNoTracking()
                .AnyAsync(t => t.EventId == eventId, ct);

            if (existing)
                return Results.Ok(new { received = true, duplicate = true, sessionId = completed.SessionId });

            var tx = new PlayerTransaction(
                eventId: eventId,
                kind: "stripe-checkout-payment",
                correlatedEventId: null,
                receipt: completed.SessionId);

            tx.AddActor(playerId, PlayerTransactionActorRole.Buyer);
            tx.AddItemChange(storeItem.Sku, storeItem.GrantQuantity * quantity, ItemOperation.Grant);
            tx.MarkApplied();

            db.PlayerTransactions.Add(tx);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new
            {
                received = true,
                applied = true,
                sessionId = completed.SessionId,
                playerTransactionId = tx.Id
            });
        }

        private static async Task<bool> VerifyAppleReceiptAsync(
            IapReceiptValidationRequest req,
            string appleSecret,
            IHttpClientFactory httpClientFactory,
            CancellationToken ct)
        {
            var payload = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["receipt-data"] = req.Receipt.Trim(),
                ["password"] = appleSecret
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

        private static async Task<IResult> CreateStripeSubscriptionCheckoutSession(
            [FromBody] CreateStripeSubscriptionCheckoutSessionRequest req,
            HttpContext httpContext,
            IAppDb db,
            IStripePaymentGateway stripeGateway,
            IOptions<StripeOptions> stripeOptionsAccessor,
            CancellationToken ct)
        {
            if (req.PlayerId == Guid.Empty)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId is required.");

            if (!TryGetAuthenticatedPlayerId(httpContext.User, out var authenticatedPlayerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "A valid bearer token is required.");

            if (authenticatedPlayerId != req.PlayerId)
                return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only create a Stripe subscription session for your own player account.");

            var tier = (req.Tier ?? string.Empty).Trim().ToLowerInvariant();
            var billingPeriod = (req.BillingPeriod ?? string.Empty).Trim().ToLowerInvariant();
            if (tier is not ("premium" or "elite"))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "tier must be 'premium' or 'elite'.");

            if (billingPeriod is not ("monthly" or "seasonal"))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "billingPeriod must be 'monthly' or 'seasonal'.");

            var stripeOptions = stripeOptionsAccessor.Value;
            var plan = stripeOptions.SubscriptionPlans.FirstOrDefault(p =>
                string.Equals(p.Tier, tier, StringComparison.OrdinalIgnoreCase)
                && string.Equals(p.BillingPeriod, billingPeriod, StringComparison.OrdinalIgnoreCase));

            if (plan is null || string.IsNullOrWhiteSpace(plan.PriceId))
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "STRIPE_SUBSCRIPTION_PLAN_NOT_CONFIGURED",
                    $"Stripe subscription pricing is not configured for {tier}/{billingPeriod}.");

            var successUrl = req.SuccessUrl ?? stripeOptions.SuccessUrl;
            var cancelUrl = req.CancelUrl ?? stripeOptions.CancelUrl;
            if (!IsValidAbsoluteHttpUrl(successUrl) || !IsValidAbsoluteHttpUrl(cancelUrl))
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "STRIPE_REDIRECT_URL_NOT_CONFIGURED",
                    "Stripe success and cancel URLs must be configured as absolute HTTP or HTTPS URLs.");

            var playerEmail = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == req.PlayerId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(ct);

            try
            {
                var checkout = await stripeGateway.CreateSubscriptionCheckoutSessionAsync(
                    new StripeSubscriptionCheckoutSessionCreateRequest(
                        req.PlayerId,
                        playerEmail,
                        tier,
                        billingPeriod,
                        plan.PriceId,
                        successUrl!,
                        cancelUrl!,
                        new Dictionary<string, string>
                        {
                            ["player_id"] = req.PlayerId.ToString(),
                            ["tier"] = tier,
                            ["billing_period"] = billingPeriod
                        }),
                    ct);

                return Results.Ok(new CreateStripeSubscriptionCheckoutSessionResponse(
                    checkout.SessionId,
                    checkout.CheckoutUrl,
                    plan.PriceId,
                    tier,
                    billingPeriod,
                    stripeOptions.PublishableKey));
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "STRIPE_NOT_READY", ex.Message);
            }
        }

        private static async Task<IResult> CreateStripeBillingPortalSession(
            [FromBody] CreateStripeBillingPortalSessionRequest req,
            HttpContext httpContext,
            IAppDb db,
            IStripePaymentGateway stripeGateway,
            IOptions<StripeOptions> stripeOptionsAccessor,
            CancellationToken ct)
        {
            if (req.PlayerId == Guid.Empty)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId is required.");

            if (!TryGetAuthenticatedPlayerId(httpContext.User, out var authenticatedPlayerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "A valid bearer token is required.");

            if (authenticatedPlayerId != req.PlayerId)
                return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only create a Stripe billing portal session for your own player account.");

            var status = await GetLatestSubscriptionStateAsync(req.PlayerId, db, ct);
            if (status is null || string.IsNullOrWhiteSpace(status.StripeCustomerId))
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "No Stripe customer was found for this player subscription.");

            var returnUrl = req.ReturnUrl ?? stripeOptionsAccessor.Value.PortalReturnUrl ?? stripeOptionsAccessor.Value.SuccessUrl;
            if (!IsValidAbsoluteHttpUrl(returnUrl))
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "STRIPE_REDIRECT_URL_NOT_CONFIGURED",
                    "Stripe portal return URL must be configured as an absolute HTTP or HTTPS URL.");

            try
            {
                var portal = await stripeGateway.CreateBillingPortalSessionAsync(
                    new StripeBillingPortalSessionCreateRequest(status.StripeCustomerId, returnUrl!),
                    ct);

                return Results.Ok(new CreateStripeBillingPortalSessionResponse(portal.SessionId, portal.Url));
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "STRIPE_NOT_READY", ex.Message);
            }
        }

        private static async Task<IResult> CreatePayPalSubscription(
            [FromBody] CreatePayPalSubscriptionRequest req,
            HttpContext httpContext,
            IAppDb db,
            IPayPalPaymentGateway payPalGateway,
            IOptions<PayPalOptions> payPalOptionsAccessor,
            CancellationToken ct)
        {
            if (req.PlayerId == Guid.Empty)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId is required.");

            if (!TryGetAuthenticatedPlayerId(httpContext.User, out var authenticatedPlayerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "A valid bearer token is required.");

            if (authenticatedPlayerId != req.PlayerId)
                return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only create a PayPal subscription for your own player account.");

            var tier = (req.Tier ?? string.Empty).Trim().ToLowerInvariant();
            var billingPeriod = (req.BillingPeriod ?? string.Empty).Trim().ToLowerInvariant();
            if (tier is not ("premium" or "elite"))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "tier must be 'premium' or 'elite'.");
            if (billingPeriod is not ("monthly" or "seasonal"))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "billingPeriod must be 'monthly' or 'seasonal'.");

            var payPalOptions = payPalOptionsAccessor.Value;
            var plan = payPalOptions.SubscriptionPlans.FirstOrDefault(p =>
                string.Equals(p.Tier, tier, StringComparison.OrdinalIgnoreCase)
                && string.Equals(p.BillingPeriod, billingPeriod, StringComparison.OrdinalIgnoreCase));

            if (plan is null || string.IsNullOrWhiteSpace(plan.PlanId))
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "PAYPAL_SUBSCRIPTION_PLAN_NOT_CONFIGURED",
                    $"PayPal subscription pricing is not configured for {tier}/{billingPeriod}.");

            var returnUrl = req.ReturnUrl ?? payPalOptions.ReturnUrl;
            var cancelUrl = req.CancelUrl ?? payPalOptions.CancelUrl;
            if (!IsValidAbsoluteHttpUrl(returnUrl) || !IsValidAbsoluteHttpUrl(cancelUrl))
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "PAYPAL_REDIRECT_URL_NOT_CONFIGURED",
                    "PayPal return and cancel URLs must be configured as absolute HTTP or HTTPS URLs.");

            var playerEmail = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == req.PlayerId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(ct);

            try
            {
                var subscription = await payPalGateway.CreateSubscriptionAsync(
                    new PayPalCreateSubscriptionRequest(
                        req.PlayerId,
                        playerEmail,
                        tier,
                        billingPeriod,
                        plan.PlanId,
                        returnUrl!,
                        cancelUrl!),
                    ct);

                return Results.Ok(new CreatePayPalSubscriptionResponse(
                    subscription.SubscriptionId,
                    subscription.Status,
                    subscription.ApproveUrl,
                    plan.PlanId,
                    tier,
                    billingPeriod,
                    payPalOptions.ClientId));
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "PAYPAL_NOT_READY", ex.Message);
            }
        }

        private static async Task<IResult> CancelPayPalSubscription(
            [FromBody] CancelPayPalSubscriptionRequest req,
            HttpContext httpContext,
            IAppDb db,
            IPayPalPaymentGateway payPalGateway,
            CancellationToken ct)
        {
            if (req.PlayerId == Guid.Empty || string.IsNullOrWhiteSpace(req.SubscriptionId))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId and subscriptionId are required.");

            if (!TryGetAuthenticatedPlayerId(httpContext.User, out var authenticatedPlayerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "A valid bearer token is required.");

            if (authenticatedPlayerId != req.PlayerId)
                return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only cancel your own PayPal subscription.");

            try
            {
                var subscription = await payPalGateway.GetSubscriptionAsync(req.SubscriptionId, ct);
                var metadata = ParsePayPalSubscriptionCustomId(subscription.CustomId);
                if (metadata is null || metadata.PlayerId != req.PlayerId)
                    return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "PayPal subscription ownership could not be confirmed.");

                await payPalGateway.CancelSubscriptionAsync(req.SubscriptionId, req.Reason ?? "Canceled by customer", ct);

                var eventId = CreateDeterministicGuid($"paypal-subscription-cancel:{req.SubscriptionId}");
                var existing = await db.PlayerTransactions.AsNoTracking().AnyAsync(t => t.EventId == eventId, ct);
                if (!existing)
                {
                    var tx = new PlayerTransaction(
                        eventId: eventId,
                        kind: "paypal-subscription-status",
                        correlatedEventId: null,
                        receipt: SerializeSubscriptionReceipt(
                            provider: "paypal",
                            tier: metadata.Tier,
                            billingPeriod: metadata.BillingPeriod,
                            subscriptionId: req.SubscriptionId,
                            customerId: null,
                            status: "CANCELLED",
                            currentPeriodEndUtc: subscription.NextBillingTimeUtc,
                            cancelAtPeriodEnd: true));
                    tx.AddActor(req.PlayerId, PlayerTransactionActorRole.Buyer);
                    tx.MarkApplied();
                    db.PlayerTransactions.Add(tx);
                    await db.SaveChangesAsync(ct);
                }

                return Results.Ok(new { subscriptionId = req.SubscriptionId, canceled = true });
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "PAYPAL_NOT_READY", ex.Message);
            }
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
                receipt: SerializeSubscriptionReceipt(
                    provider: "manual",
                    tier: tier,
                    billingPeriod: period,
                    subscriptionId: req.ExternalTransactionId,
                    customerId: null,
                    status: "active",
                    currentPeriodEndUtc: null,
                    cancelAtPeriodEnd: false));
            tx.AddActor(req.PlayerId, PlayerTransactionActorRole.Buyer);
            tx.MarkApplied();

            db.PlayerTransactions.Add(tx);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new SubscriptionStatusDto(
                PlayerId: req.PlayerId,
                IsActive: true,
                Tier: tier,
                BillingPeriod: period,
                ActivatedAtUtc: tx.CompletedAtUtc ?? tx.CreatedAtUtc,
                Provider: "manual",
                ProviderSubscriptionId: req.ExternalTransactionId,
                ProviderCustomerId: null,
                ProviderStatus: "active",
                StripeSubscriptionId: req.ExternalTransactionId,
                StripeCustomerId: null,
                StripeStatus: "active",
                CurrentPeriodEndUtc: null,
                CancelAtPeriodEnd: false));
        }

        private static async Task<IResult> GetSubscriptionStatus(
            [FromRoute] Guid playerId,
            IAppDb db,
            CancellationToken ct)
        {
            if (playerId == Guid.Empty)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "playerId cannot be empty.");

            var latest = await GetLatestSubscriptionStateAsync(playerId, db, ct);
            if (latest is null)
                return Results.Ok(new SubscriptionStatusDto(playerId, false, null, null, null));
            return Results.Ok(latest);
        }

        private static bool TryGetAuthenticatedPlayerId(ClaimsPrincipal user, out Guid playerId)
        {
            playerId = Guid.Empty;
            var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;

            return raw is not null && Guid.TryParse(raw, out playerId);
        }

        private static bool IsValidAbsoluteHttpUrl(string? value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static Guid CreateDeterministicGuid(string source)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
            var bytes = new byte[16];
            Array.Copy(hash, bytes, bytes.Length);
            return new Guid(bytes);
        }

        private static async Task<SubscriptionStatusDto?> GetLatestSubscriptionStateAsync(
            Guid playerId,
            IAppDb db,
            CancellationToken ct)
        {
            var latest = await db.PlayerTransactions
                .AsNoTracking()
                .Where(t =>
                    (t.Kind == "battle-pass-subscription"
                     || t.Kind == "stripe-subscription-activated"
                     || t.Kind == "stripe-subscription-status"
                     || t.Kind == "paypal-subscription-activated"
                     || t.Kind == "paypal-subscription-status")
                    && t.Status == PlayerTransactionStatus.Applied
                    && t.Actors.Any(a => a.PlayerId == playerId))
                .OrderByDescending(t => t.CompletedAtUtc ?? t.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (latest is null)
                return null;

            var receipt = ParseSubscriptionReceipt(latest.Receipt);
            var stripeStatus = receipt.Status ?? "active";
            var isActive = string.Equals(stripeStatus, "active", StringComparison.OrdinalIgnoreCase)
                || string.Equals(stripeStatus, "trialing", StringComparison.OrdinalIgnoreCase);

            return new SubscriptionStatusDto(
                PlayerId: playerId,
                IsActive: isActive,
                Tier: receipt.Tier ?? "premium",
                BillingPeriod: receipt.BillingPeriod ?? "monthly",
                ActivatedAtUtc: latest.CompletedAtUtc ?? latest.CreatedAtUtc,
                Provider: receipt.Provider,
                ProviderSubscriptionId: receipt.SubscriptionId,
                ProviderCustomerId: receipt.CustomerId,
                ProviderStatus: stripeStatus,
                StripeSubscriptionId: receipt.SubscriptionId,
                StripeCustomerId: receipt.CustomerId,
                StripeStatus: stripeStatus,
                CurrentPeriodEndUtc: receipt.CurrentPeriodEndUtc,
                CancelAtPeriodEnd: receipt.CancelAtPeriodEnd);
        }

        private static string SerializeSubscriptionReceipt(
            string provider,
            string? tier,
            string? billingPeriod,
            string? subscriptionId,
            string? customerId,
            string? status,
            DateTimeOffset? currentPeriodEndUtc,
            bool cancelAtPeriodEnd)
        {
            return JsonSerializer.Serialize(new
            {
                provider,
                tier,
                billingPeriod,
                subscriptionId,
                customerId,
                status,
                currentPeriodEndUtc,
                cancelAtPeriodEnd
            });
        }

        private static SubscriptionReceipt ParseSubscriptionReceipt(string? receipt)
        {
            if (string.IsNullOrWhiteSpace(receipt))
                return new SubscriptionReceipt();

            try
            {
                using var doc = JsonDocument.Parse(receipt);
                var root = doc.RootElement;
                return new SubscriptionReceipt
                {
                    Tier = root.TryGetProperty("tier", out var tier) ? tier.GetString() : null,
                    Provider = root.TryGetProperty("provider", out var provider) ? provider.GetString() : null,
                    BillingPeriod = root.TryGetProperty("billingPeriod", out var period) ? period.GetString() : null,
                    SubscriptionId = root.TryGetProperty("subscriptionId", out var subId) ? subId.GetString() : null,
                    CustomerId = root.TryGetProperty("customerId", out var custId) ? custId.GetString() : null,
                    Status = root.TryGetProperty("status", out var status) ? status.GetString() : null,
                    CurrentPeriodEndUtc = root.TryGetProperty("currentPeriodEndUtc", out var end)
                        && end.ValueKind != JsonValueKind.Null
                        ? end.GetDateTimeOffset()
                        : null,
                    CancelAtPeriodEnd = root.TryGetProperty("cancelAtPeriodEnd", out var cancel)
                        && cancel.ValueKind == JsonValueKind.True
                };
            }
            catch
            {
                var parts = receipt.Split(':', StringSplitOptions.RemoveEmptyEntries);
                return new SubscriptionReceipt
                {
                    Tier = parts.Length > 0 ? parts[0] : null,
                    Provider = "legacy",
                    BillingPeriod = parts.Length > 1 ? parts[1] : null,
                    SubscriptionId = parts.Length > 2 ? parts[2] : null,
                    Status = "active"
                };
            }
        }

        private sealed class SubscriptionReceipt
        {
            public string? Tier { get; init; }

            public string? Provider { get; init; }

            public string? BillingPeriod { get; init; }

            public string? SubscriptionId { get; init; }

            public string? CustomerId { get; init; }

            public string? Status { get; init; }

            public DateTimeOffset? CurrentPeriodEndUtc { get; init; }

            public bool CancelAtPeriodEnd { get; init; }
        }

        private static PayPalOrderMetadata? ParsePayPalCustomId(string? customId)
        {
            if (string.IsNullOrWhiteSpace(customId))
                return null;

            var parts = customId.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
                return null;

            if (!Guid.TryParseExact(parts[0], "N", out var playerId))
                return null;

            return int.TryParse(parts[2], out var quantity)
                ? new PayPalOrderMetadata(playerId, parts[1], quantity)
                : null;
        }

        private static PayPalSubscriptionMetadata? ParsePayPalSubscriptionCustomId(string? customId)
        {
            if (string.IsNullOrWhiteSpace(customId))
                return null;

            var parts = customId.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
                return null;

            if (!Guid.TryParseExact(parts[0], "N", out var playerId))
                return null;

            return new PayPalSubscriptionMetadata(playerId, parts[1], parts[2]);
        }

        private sealed record PayPalOrderMetadata(Guid PlayerId, string Sku, int Quantity);

        private sealed record PayPalSubscriptionMetadata(Guid PlayerId, string Tier, string BillingPeriod);

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
