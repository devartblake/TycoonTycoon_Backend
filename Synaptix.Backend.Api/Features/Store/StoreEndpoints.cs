using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mediator;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Api.Security;
using Synaptix.Backend.Api.Payments.PayPal;
using Synaptix.Backend.Api.Payments.Stripe;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Avatars;
using Synaptix.Backend.Application.Personalization;
using Synaptix.Backend.Application.PlayerTransactions;
using Synaptix.Backend.Application.Store;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Domain.Personalization;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Store
{
    public static class StoreEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/store").WithTags("Store");

            g.MapGet("/catalog", GetCatalog);
            g.MapGet("/catalog/{sku}", GetItem);
            g.MapGet("/premium", GetPremium).RequireAuthorization();
            g.MapGet("/rewards/{playerId:guid}", GetRewards).RequireAuthorization();
            g.MapPost("/rewards/{playerId:guid}/claim/{rewardId}", ClaimReward).RequireAuthorization();
            g.MapGet("/system/status", GetSystemStatus);
            g.MapGet("/inventory/{playerId:guid}", GetInventory).RequireAuthorization();
            g.MapGet("/subscription/status/{playerId:guid}", GetSubscriptionStatus).RequireAuthorization();
            g.MapPost("/subscription/activate", ActivateSubscription).RequireAuthorization();
            g.MapPost("/subscription/checkout/session", CreateStripeSubscriptionCheckoutSession).RequireAuthorization().RequireSecureChannel();
            g.MapPost("/subscription/portal/session", CreateStripeBillingPortalSession).RequireAuthorization();
            g.MapPost("/subscription/paypal/create", CreatePayPalSubscription).RequireAuthorization().RequireSecureChannel();
            g.MapPost("/subscription/paypal/cancel", CancelPayPalSubscription).RequireAuthorization();
            g.MapGet("/daily", GetDailyStore).RequireAuthorization();
            g.MapGet("/hub", GetStoreHub).RequireAuthorization();
            g.MapGet("/special-offers", GetSpecialOffers).RequireAuthorization();
            g.MapGet("/catalog/{playerId:guid}", GetPlayerCatalog).RequireAuthorization();
            g.MapPost("/purchase", Purchase).RequireAuthorization().RequireSecureChannel();
            g.MapPost("/payments/checkout/session", CreateStripeCheckoutSession).RequireAuthorization().RequireSecureChannel();
            g.MapPost("/payments/paypal/order", CreatePayPalOrder).RequireAuthorization().RequireSecureChannel();
            g.MapPost("/payments/paypal/capture", CapturePayPalOrder).RequireAuthorization().RequireSecureChannel();
            g.MapPost("/payments/webhook", HandleStripeWebhook);        // Stripe webhook — not from the app
            g.MapPost("/payments/paypal/webhook", HandlePayPalWebhook); // PayPal webhook — not from the app
            g.MapPost("/iap/validate", ValidateIapReceipt).RequireAuthorization().RequireSecureChannel();
            g.MapGet("/recommendations/{playerId:guid}", GetStoreRecommendations).RequireAuthorization();
        }

        private static async Task<IResult> GetCatalog(
            [FromQuery] string? itemType,
            [FromQuery] string? category,
            HttpContext httpContext,
            IMediator mediator,
            IAppDb db,
            IOptions<StorePremiumOptions> premiumOptionsAccessor,
            CancellationToken ct)
        {
            if (string.Equals(category, "avatar", StringComparison.OrdinalIgnoreCase))
            {
                TryGetAuthenticatedPlayerId(httpContext.User, out var playerId);
                var catalog = await mediator.Send(
                    new GetAvatarCatalog(playerId == Guid.Empty ? null : playerId), ct);
                return Results.Ok(new { items = catalog.Items });
            }

            var normalizedItemType = string.IsNullOrWhiteSpace(itemType) ? null : itemType.Trim();

            var query = db.StoreItems
                .AsNoTracking()
                .Where(i => i.IsActive);

            if (!string.IsNullOrWhiteSpace(normalizedItemType))
                query = query.Where(i => i.ItemType == normalizedItemType);

            var items = await query
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Name)
                .Select(i => new StoreItemDto(
                    i.Id, i.Sku, i.Name, i.Description, i.ItemType,
                    i.PriceCoins, i.PriceDiamonds, i.GrantQuantity,
                    i.MaxPerPlayer, i.MediaKey, i.SortOrder))
                .ToListAsync(ct);

            items.AddRange(BuildPremiumCatalogFallbackItems(
                premiumOptionsAccessor.Value,
                normalizedItemType,
                items.Select(i => i.Sku).ToHashSet(StringComparer.OrdinalIgnoreCase)));

            items = items
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Name)
                .ToList();

            return Results.Ok(new StoreCatalogDto(items, items.Count));
        }

        private static async Task<IResult> GetSystemStatus(
            IAppDb db,
            IConfiguration configuration,
            CancellationToken ct)
        {
            var status = await StoreSystemStatusSupport.GetStatusAsync(db, configuration, ct);
            return Results.Ok(status);
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

        private static IResult GetPremium(
            IMemoryCache cache,
            IOptions<StorePremiumOptions> premiumOptionsAccessor)
        {
            const string cacheKey = "store:premium:v1";

            var payload = cache.GetOrCreate(cacheKey, entry =>
            {
                var options = premiumOptionsAccessor.Value;
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Max(1, options.CacheMinutes));
                return BuildPremiumStoreDto(options);
            });

            return Results.Ok(payload);
        }

        private static async Task<IResult> GetRewards(
            [FromRoute] Guid playerId,
            HttpContext httpContext,
            IAppDb db,
            IOptions<StorePremiumOptions> premiumOptionsAccessor,
            CancellationToken ct)
        {
            var ownershipGate = await EnsureRewardAccessAsync(playerId, httpContext.User, db, ct);
            if (ownershipGate is not null)
                return ownershipGate;

            var rewardCenter = await BuildRewardCenterAsync(
                playerId,
                db,
                premiumOptionsAccessor.Value,
                DateTimeOffset.UtcNow,
                ct);

            return Results.Ok(rewardCenter);
        }

        private static async Task<IResult> ClaimReward(
            [FromRoute] Guid playerId,
            [FromRoute] string rewardId,
            HttpContext httpContext,
            IAppDb db,
            PlayerTransactionService txnService,
            IOptions<StorePremiumOptions> premiumOptionsAccessor,
            CancellationToken ct)
        {
            var ownershipGate = await EnsureRewardAccessAsync(playerId, httpContext.User, db, ct);
            if (ownershipGate is not null)
                return ownershipGate;

            var now = DateTimeOffset.UtcNow;
            var dayStartUtc = StartOfUtcDay(now);
            var nextResetUtc = dayStartUtc.AddDays(1);
            var premiumOptions = premiumOptionsAccessor.Value;
            var rewardPolicies = premiumOptions.RewardPolicies ?? new StorePremiumRewardPolicyOptions();
            var normalizedRewardId = (rewardId ?? string.Empty).Trim().ToLowerInvariant();

            return normalizedRewardId switch
            {
                "daily-checkin" => await ClaimDailyCheckinAsync(
                    playerId,
                    db,
                    txnService,
                    rewardPolicies,
                    dayStartUtc,
                    nextResetUtc,
                    ct),
                "watch-ad" => await ClaimWatchAdAsync(
                    playerId,
                    db,
                    txnService,
                    rewardPolicies,
                    dayStartUtc,
                    now,
                    ct),
                _ => ApiResponses.Error(
                    StatusCodes.Status404NotFound,
                    "not_found",
                    $"Reward '{rewardId}' was not found.")
            };
        }

        private static async Task<IResult> GetPlayerCatalog(
            [FromRoute] Guid playerId,
            [FromQuery] string? itemType,
            [FromQuery] string? category,
            HttpContext httpContext,
            IStoreStockService stockService,
            CancellationToken ct)
        {
            if (!TryGetAuthenticatedPlayerId(httpContext.User, out var jwtPlayerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");
            if (jwtPlayerId != playerId)
                return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Cannot view another player's catalog.");

            var catalog = await stockService.GetCatalogForPlayerAsync(playerId, itemType, category, ct);
            return Results.Ok(catalog);
        }

        private static async Task<IResult> GetStoreRecommendations(
            [FromRoute] Guid playerId,
            HttpContext httpContext,
            IPersonalizationService personalization,
            CancellationToken ct)
        {
            if (!TryGetAuthenticatedPlayerId(httpContext.User, out var jwtPlayerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");
            if (jwtPlayerId != playerId)
                return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Cannot view another player's recommendations.");

            var result = await personalization.GetStoreRecommendationsAsync(playerId, ct);
            return Results.Ok(result);
        }

        private static async Task<IResult> GetStoreHub(
            HttpContext httpContext,
            IStoreStockService stockService,
            CancellationToken ct)
        {
            if (!TryGetAuthenticatedPlayerId(httpContext.User, out var playerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var hub = await stockService.GetHubAsync(playerId, ct);
            return Results.Ok(hub);
        }

        private static async Task<IResult> GetSpecialOffers(
            HttpContext httpContext,
            IStoreStockService stockService,
            IAppDb db,
            CancellationToken ct)
        {
            if (TryGetAuthenticatedPlayerId(httpContext.User, out var playerId))
            {
                var frustration = await db.PlayerMindProfiles
                    .AsNoTracking()
                    .Where(p => p.PlayerId == playerId)
                    .Select(p => (decimal?)p.FrustrationRiskScore)
                    .FirstOrDefaultAsync(ct);

                if (frustration >= 0.75m)
                    return Results.Ok(new SpecialOffersResponseDto([]));
            }

            var offers = await stockService.GetSpecialOffersAsync(ct);
            return Results.Ok(new SpecialOffersResponseDto(offers));
        }

        private static async Task<IResult> GetDailyStore(
            HttpContext httpContext,
            IStoreStockService stockService,
            CancellationToken ct)
        {
            if (!TryGetAuthenticatedPlayerId(httpContext.User, out var playerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var items = await stockService.GetDailyItemsAsync(playerId, ct);
            var now = DateTimeOffset.UtcNow;
            var resetsAt = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);
            return Results.Ok(new DailyStoreResponseDto(now, resetsAt, items));
        }

        private static async Task<IResult> Purchase(
            [FromBody] StorePurchaseRequest req,
            HttpContext httpContext,
            IAppDb db,
            IConfiguration configuration,
            PlayerTransactionService txnService,
            IStoreStockService stockService,
            IPlayerMindProfileService mindProfiles,
            CancellationToken ct)
        {
            var storeEnabled = await EnsureStoreEnabledAsync(db, configuration, ct);
            if (storeEnabled is not null)
                return storeEnabled;

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

            // Check stock policy — returns "store_item_out_of_stock" if the player has exhausted their quota
            var stockError = await stockService.CheckStockAsync(req.PlayerId, req.Sku, req.Quantity, ct);
            if (stockError is not null)
                return ApiResponses.Error(StatusCodes.Status409Conflict, stockError,
                    "This item is out of stock for the current period.");

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

            if (result.Status == "Applied")
            {
                await stockService.ConsumeStockAsync(req.PlayerId, req.Sku, req.Quantity, ct);
                try
                {
                    await mindProfiles.RecordEventAsync(req.PlayerId, new PlayerBehaviorEventDto(
                        EventType: "store_item_purchased",
                        EventSource: "store",
                        Category: storeItem.ItemType,
                        Difficulty: null,
                        Mode: null,
                        Metadata: new Dictionary<string, object>
                        {
                            ["sku"] = req.Sku,
                            ["quantity"] = req.Quantity,
                            ["currency"] = currency!,
                            ["totalPrice"] = totalPrice
                        },
                        OccurredAt: DateTimeOffset.UtcNow), ct);
                }
                catch { /* personalization must never break purchase flow */ }
            }

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

            var itemChanges = await db.PlayerTransactions
                .AsNoTracking()
                .Where(t => t.Status == PlayerTransactionStatus.Applied
                            && t.Actors.Any(a => a.PlayerId == playerId))
                .SelectMany(t => t.ItemChanges)
                .Where(i => i.ItemType.StartsWith("cosmetic:", StringComparison.OrdinalIgnoreCase)
                            || i.ItemType.StartsWith("powerup:", StringComparison.OrdinalIgnoreCase))
                .ToListAsync(ct);

            var items = itemChanges
                .GroupBy(i => i.ItemType)
                .Select(g => new PlayerInventoryItemDto(
                    g.Key,
                    g.Sum(i => i.Operation == ItemOperation.Revoke ? -i.Quantity : i.Quantity)))
                .Where(x => x.Quantity > 0)
                .OrderBy(x => x.ItemType)
                .ToList();

            return Results.Ok(new PlayerInventoryDto(playerId, items, items.Count));
        }

        private static async Task<IResult> ValidateIapReceipt(
            [FromBody] IapReceiptValidationRequest req,
            IAppDb db,
            IConfiguration cfg,
            IHttpClientFactory httpClientFactory,
            CancellationToken ct)
        {
            var paymentsEnabled = await EnsurePaymentsEnabledAsync(db, cfg, ct);
            if (paymentsEnabled is not null)
                return paymentsEnabled;

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

                if (!strictValid
                    && cfg.GetValue("Testing:UseInMemoryDb", false)
                    && !string.IsNullOrWhiteSpace(req.ProductId)
                    && !string.IsNullOrWhiteSpace(req.ExternalTransactionId))
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
            IConfiguration configuration,
            IStripePaymentGateway stripeGateway,
            IOptions<StripeOptions> stripeOptionsAccessor,
            CancellationToken ct)
        {
            var stripeEnabled = await EnsureStripeEnabledAsync(db, configuration, ct);
            if (stripeEnabled is not null)
                return stripeEnabled;

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
            IConfiguration configuration,
            IPayPalPaymentGateway payPalGateway,
            IOptions<PayPalOptions> payPalOptionsAccessor,
            CancellationToken ct)
        {
            var payPalEnabled = await EnsurePayPalEnabledAsync(db, configuration, ct);
            if (payPalEnabled is not null)
                return payPalEnabled;

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
            IConfiguration configuration,
            IPayPalPaymentGateway payPalGateway,
            CancellationToken ct)
        {
            var payPalEnabled = await EnsurePayPalEnabledAsync(db, configuration, ct);
            if (payPalEnabled is not null)
                return payPalEnabled;

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

            if (string.Equals(webhookEvent.EventType, global::Stripe.EventTypes.CustomerSubscriptionUpdated, StringComparison.Ordinal)
                || string.Equals(webhookEvent.EventType, global::Stripe.EventTypes.CustomerSubscriptionDeleted, StringComparison.Ordinal))
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

            if (!string.Equals(webhookEvent.EventType, global::Stripe.EventTypes.CheckoutSessionCompleted, StringComparison.Ordinal))
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
            IConfiguration configuration,
            IStripePaymentGateway stripeGateway,
            IOptions<StripeOptions> stripeOptionsAccessor,
            CancellationToken ct)
        {
            var stripeEnabled = await EnsureStripeEnabledAsync(db, configuration, ct);
            if (stripeEnabled is not null)
                return stripeEnabled;

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
            IConfiguration configuration,
            IPayPalPaymentGateway payPalGateway,
            IOptions<PayPalOptions> payPalOptionsAccessor,
            CancellationToken ct)
        {
            var payPalEnabled = await EnsurePayPalEnabledAsync(db, configuration, ct);
            if (payPalEnabled is not null)
                return payPalEnabled;

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
            IConfiguration configuration,
            CancellationToken ct)
        {
            var storeEnabled = await EnsureStoreEnabledAsync(db, configuration, ct);
            if (storeEnabled is not null)
                return storeEnabled;

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

        private static PremiumStoreDto BuildPremiumStoreDto(StorePremiumOptions options)
        {
            var adFree = options.AdFree ?? new StorePremiumAdFreeOptions();
            var rewardCenter = options.RewardCenter ?? new StorePremiumRewardCenterOptions();
            var sale = options.FlashSale;

            return new PremiumStoreDto(
                new PremiumAdFreeDto(
                    adFree.Title,
                    adFree.Subtitle,
                    adFree.Benefits,
                    adFree.Plans.Select(plan => new PremiumAdFreePlanDto(
                        plan.Id,
                        plan.Title,
                        plan.Subtitle,
                        plan.PriceLabel,
                        plan.Badge,
                        plan.AccentColor,
                        plan.IsBestValue,
                        plan.Sku)).ToList()),
                sale is not null && sale.IsActive
                    ? new PremiumSaleInfoDto(
                        sale.Badge,
                        sale.Title,
                        sale.Subtitle,
                        sale.CtaLabel,
                        sale.GradientStart,
                        sale.GradientEnd,
                        sale.Benefits)
                    : null,
                new RewardCenterDto(
                    rewardCenter.Title,
                    rewardCenter.Subtitle,
                    rewardCenter.Cards.Select(card =>
                    {
                        var rewardId = string.IsNullOrWhiteSpace(card.RewardId) ? "unknown" : card.RewardId;
                        return new RewardCardDto(
                        rewardId,
                        card.Title,
                        BuildDefaultRewardSubtitle(rewardId),
                        card.RewardLabel,
                        "available",
                        card.GradientStart,
                        card.GradientEnd,
                        0,
                        rewardId == "daily-checkin" || rewardId == "watch-ad",
                        rewardId == "watch-ad" ? options.RewardPolicies.WatchAdDailyCap : null,
                        rewardId == "watch-ad" ? options.RewardPolicies.WatchAdDailyCap : null,
                        null);
                    }).ToList()));
        }

        private static IReadOnlyList<StoreItemDto> BuildPremiumCatalogFallbackItems(
            StorePremiumOptions options,
            string? requestedItemType,
            HashSet<string> existingSkus)
        {
            if (!ShouldIncludePremiumCatalogFallback(requestedItemType))
                return Array.Empty<StoreItemDto>();

            var plans = options.AdFree?.Plans ?? new List<StorePremiumPlanOptions>();
            var result = new List<StoreItemDto>();

            for (var i = 0; i < plans.Count; i++)
            {
                var plan = plans[i];
                var sku = string.IsNullOrWhiteSpace(plan.Sku) ? plan.Id : plan.Sku;
                if (string.IsNullOrWhiteSpace(sku) || existingSkus.Contains(sku))
                    continue;

                result.Add(new StoreItemDto(
                    Id: CreateDeterministicGuid($"store-catalog:premium:{sku}"),
                    Sku: sku.Trim(),
                    Name: string.IsNullOrWhiteSpace(plan.Title) ? plan.Id : plan.Title.Trim(),
                    Description: BuildPremiumCatalogDescription(plan),
                    ItemType: "premium-subscription",
                    PriceCoins: 0,
                    PriceDiamonds: 0,
                    GrantQuantity: 1,
                    MaxPerPlayer: 0,
                    MediaKey: null,
                    SortOrder: 10_000 + i));
            }

            return result;
        }

        private static bool ShouldIncludePremiumCatalogFallback(string? requestedItemType)
        {
            if (string.IsNullOrWhiteSpace(requestedItemType))
                return true;

            return requestedItemType.Equals("premium", StringComparison.OrdinalIgnoreCase)
                   || requestedItemType.Equals("premium-subscription", StringComparison.OrdinalIgnoreCase)
                   || requestedItemType.Equals("subscription", StringComparison.OrdinalIgnoreCase)
                   || requestedItemType.Equals("ad-free", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildPremiumCatalogDescription(StorePremiumPlanOptions plan)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(plan.Subtitle))
                parts.Add(plan.Subtitle.Trim());

            if (!string.IsNullOrWhiteSpace(plan.PriceLabel))
                parts.Add(plan.PriceLabel.Trim());

            if (!string.IsNullOrWhiteSpace(plan.Badge))
                parts.Add(plan.Badge.Trim());

            return parts.Count == 0
                ? "Premium subscription plan."
                : string.Join(" ", parts);
        }

        private static async Task<RewardCenterDto> BuildRewardCenterAsync(
            Guid playerId,
            IAppDb db,
            StorePremiumOptions premiumOptions,
            DateTimeOffset now,
            CancellationToken ct)
        {
            var options = premiumOptions.RewardCenter ?? new StorePremiumRewardCenterOptions();
            var policies = premiumOptions.RewardPolicies ?? new StorePremiumRewardPolicyOptions();
            var claimRows = await db.PlayerTransactions
                .AsNoTracking()
                .Where(t =>
                    t.Status == PlayerTransactionStatus.Applied
                    && t.Actors.Any(a => a.PlayerId == playerId)
                    && (t.Kind == DailyCheckinTransactionKind || t.Kind == WatchAdTransactionKind))
                .Select(t => new RewardClaimHistoryRow(
                    t.Kind,
                    t.CompletedAtUtc ?? t.CreatedAtUtc))
                .ToListAsync(ct);

            var dailyCheckinClaimDates = claimRows
                .Where(x => x.Kind == DailyCheckinTransactionKind)
                .Select(x => StartOfUtcDay(x.OccurredAtUtc))
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();

            var dailyCheckinState = BuildDailyCheckinState(
                dailyCheckinClaimDates,
                policies,
                now);

            var watchAdClaimsToday = claimRows.Count(x =>
                x.Kind == WatchAdTransactionKind
                && x.OccurredAtUtc >= StartOfUtcDay(now)
                && x.OccurredAtUtc < StartOfUtcDay(now).AddDays(1));

            var watchAdState = BuildWatchAdState(watchAdClaimsToday, policies);

            var cards = options.Cards.Select(card =>
            {
                var rewardId = string.IsNullOrWhiteSpace(card.RewardId) ? "unknown" : card.RewardId;
                var normalizedRewardId = rewardId.Trim().ToLowerInvariant();
                return normalizedRewardId switch
                {
                    "daily-checkin" => new RewardCardDto(
                        rewardId,
                        card.Title,
                        dailyCheckinState.Subtitle,
                        card.RewardLabel,
                        dailyCheckinState.Availability,
                        card.GradientStart,
                        card.GradientEnd,
                        dailyCheckinState.Progress,
                        dailyCheckinState.IsClaimAvailable,
                        null,
                        7,
                        dailyCheckinState.NextAvailableAtUtc),
                    "watch-ad" => new RewardCardDto(
                        rewardId,
                        card.Title,
                        watchAdState.Subtitle,
                        card.RewardLabel,
                        watchAdState.Availability,
                        card.GradientStart,
                        card.GradientEnd,
                        watchAdState.Progress,
                        watchAdState.IsClaimAvailable,
                        watchAdState.RemainingClaims,
                        policies.WatchAdDailyCap,
                        watchAdState.NextAvailableAtUtc),
                    _ => new RewardCardDto(
                        rewardId,
                        card.Title,
                        BuildDefaultRewardSubtitle(rewardId),
                        card.RewardLabel,
                        "unavailable",
                        card.GradientStart,
                        card.GradientEnd,
                        0,
                        false,
                        null,
                        null,
                        null)
                };
            }).ToList();

            return new RewardCenterDto(options.Title, options.Subtitle, cards);
        }

        private static async Task<IResult> ClaimDailyCheckinAsync(
            Guid playerId,
            IAppDb db,
            PlayerTransactionService txnService,
            StorePremiumRewardPolicyOptions policies,
            DateTimeOffset dayStartUtc,
            DateTimeOffset nextResetUtc,
            CancellationToken ct)
        {
            var claimedToday = await db.PlayerTransactions
                .AsNoTracking()
                .AnyAsync(t =>
                    t.Status == PlayerTransactionStatus.Applied
                    && t.Kind == DailyCheckinTransactionKind
                    && t.Actors.Any(a => a.PlayerId == playerId)
                    && (t.CompletedAtUtc ?? t.CreatedAtUtc) >= dayStartUtc
                    && (t.CompletedAtUtc ?? t.CreatedAtUtc) < nextResetUtc,
                    ct);

            if (claimedToday)
            {
                return ApiResponses.Error(
                    StatusCodes.Status409Conflict,
                    "already_claimed",
                    "Daily check-in has already been claimed for today.");
            }

            var claimDates = await db.PlayerTransactions
                .AsNoTracking()
                .Where(t =>
                    t.Status == PlayerTransactionStatus.Applied
                    && t.Kind == DailyCheckinTransactionKind
                    && t.Actors.Any(a => a.PlayerId == playerId))
                .Select(t => t.CompletedAtUtc ?? t.CreatedAtUtc)
                .ToListAsync(ct);

            var distinctClaimDays = claimDates
                .Select(StartOfUtcDay)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();

            var currentState = BuildDailyCheckinState(distinctClaimDays, policies, dayStartUtc);
            var newStreak = currentState.LastClaimDateUtc == dayStartUtc.AddDays(-1)
                ? currentState.CurrentStreak + 1
                : 1;

            var eventId = CreateDeterministicGuid($"store-reward:{playerId}:daily-checkin:{dayStartUtc:yyyyMMdd}");
            var result = await txnService.ExecuteAsync(
                new CreatePlayerTransactionRequest(
                    EventId: eventId,
                    Kind: DailyCheckinTransactionKind,
                    Receipt: JsonSerializer.Serialize(new
                    {
                        rewardId = "daily-checkin",
                        claimDateUtc = dayStartUtc,
                        streak = newStreak
                    }),
                    Actors: new[]
                    {
                        new PlayerTransactionActorDto(playerId, "recipient")
                    },
                    CurrencyChanges: new[]
                    {
                        new PlayerTransactionCurrencyDto(
                            playerId,
                            new[]
                            {
                                new EconomyLineDto(CurrencyType.Coins, policies.DailyCheckinCoins)
                            })
                    },
                    Note: $"Premium reward claim: daily-checkin day {newStreak}"),
                ct);

            if (result.Status is not "Applied")
            {
                return ApiResponses.Error(
                    StatusCodes.Status409Conflict,
                    "already_claimed",
                    "Daily check-in has already been claimed for today.");
            }

            var newBalance = result.EconomyResults.FirstOrDefault()?.BalanceCoins ?? 0;
            return Results.Ok(new ClaimStoreRewardResponseDto(
                RewardId: "daily-checkin",
                CoinsAwarded: policies.DailyCheckinCoins,
                NewBalance: newBalance,
                Status: "claimed",
                ClaimedAtUtc: DateTimeOffset.UtcNow,
                NextAvailableAtUtc: nextResetUtc,
                CurrentStreak: newStreak,
                RemainingClaims: null));
        }

        private static async Task<IResult> ClaimWatchAdAsync(
            Guid playerId,
            IAppDb db,
            PlayerTransactionService txnService,
            StorePremiumRewardPolicyOptions policies,
            DateTimeOffset dayStartUtc,
            DateTimeOffset now,
            CancellationToken ct)
        {
            var nextResetUtc = dayStartUtc.AddDays(1);
            var claimsToday = await db.PlayerTransactions
                .AsNoTracking()
                .CountAsync(t =>
                    t.Status == PlayerTransactionStatus.Applied
                    && t.Kind == WatchAdTransactionKind
                    && t.Actors.Any(a => a.PlayerId == playerId)
                    && (t.CompletedAtUtc ?? t.CreatedAtUtc) >= dayStartUtc
                    && (t.CompletedAtUtc ?? t.CreatedAtUtc) < nextResetUtc,
                    ct);

            if (claimsToday >= policies.WatchAdDailyCap)
            {
                return ApiResponses.Error(
                    StatusCodes.Status409Conflict,
                    "already_claimed",
                    "The watch-ad reward has reached its daily claim cap.");
            }

            var claimOrdinal = claimsToday + 1;
            var eventId = CreateDeterministicGuid($"store-reward:{playerId}:watch-ad:{dayStartUtc:yyyyMMdd}:{claimOrdinal}");
            var result = await txnService.ExecuteAsync(
                new CreatePlayerTransactionRequest(
                    EventId: eventId,
                    Kind: WatchAdTransactionKind,
                    Receipt: JsonSerializer.Serialize(new
                    {
                        rewardId = "watch-ad",
                        claimDateUtc = dayStartUtc,
                        ordinal = claimOrdinal
                    }),
                    Actors: new[]
                    {
                        new PlayerTransactionActorDto(playerId, "recipient")
                    },
                    CurrencyChanges: new[]
                    {
                        new PlayerTransactionCurrencyDto(
                            playerId,
                            new[]
                            {
                                new EconomyLineDto(CurrencyType.Coins, policies.WatchAdCoins)
                            })
                    },
                    Note: $"Premium reward claim: watch-ad #{claimOrdinal}"),
                ct);

            if (result.Status is not "Applied")
            {
                return ApiResponses.Error(
                    StatusCodes.Status409Conflict,
                    "already_claimed",
                    "The watch-ad reward has already been claimed for this slot.");
            }

            var newBalance = result.EconomyResults.FirstOrDefault()?.BalanceCoins ?? 0;
            var remainingClaims = Math.Max(0, policies.WatchAdDailyCap - claimOrdinal);
            return Results.Ok(new ClaimStoreRewardResponseDto(
                RewardId: "watch-ad",
                CoinsAwarded: policies.WatchAdCoins,
                NewBalance: newBalance,
                Status: "claimed",
                ClaimedAtUtc: now,
                NextAvailableAtUtc: null,
                CurrentStreak: null,
                RemainingClaims: remainingClaims));
        }

        private static async Task<IResult?> EnsureRewardAccessAsync(
            Guid playerId,
            ClaimsPrincipal user,
            IAppDb db,
            CancellationToken ct)
        {
            if (playerId == Guid.Empty)
            {
                return ApiResponses.Error(
                    StatusCodes.Status400BadRequest,
                    "validation_error",
                    "playerId cannot be empty.");
            }

            if (!TryGetAuthenticatedPlayerId(user, out var authenticatedPlayerId))
            {
                return ApiResponses.Error(
                    StatusCodes.Status401Unauthorized,
                    "unauthorized",
                    "A valid bearer token is required.");
            }

            if (authenticatedPlayerId != playerId)
            {
                return ApiResponses.Error(
                    StatusCodes.Status403Forbidden,
                    "forbidden",
                    "You can only access rewards for your own player account.");
            }

            var playerExists = await db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == playerId, ct);

            return !playerExists
                ? ApiResponses.Error(StatusCodes.Status404NotFound, "not_found", "Player not found.")
                : null;
        }

        private static DailyCheckinRewardState BuildDailyCheckinState(
            IReadOnlyList<DateTimeOffset> claimDaysUtc,
            StorePremiumRewardPolicyOptions policies,
            DateTimeOffset now)
        {
            var today = StartOfUtcDay(now);
            var currentStreak = CountConsecutiveDays(claimDaysUtc);
            var claimedToday = claimDaysUtc.Count > 0 && claimDaysUtc[0] == today;
            var lastClaimDateUtc = claimDaysUtc.FirstOrDefault();
            var nextDayNumber = claimedToday
                ? Math.Min(currentStreak + 1, 7)
                : Math.Min((currentStreak > 0 && lastClaimDateUtc == today.AddDays(-1)) ? currentStreak + 1 : 1, 7);

            var subtitle = claimedToday
                ? $"Checked in for day {Math.Min(currentStreak, 7)}. Come back tomorrow."
                : $"Day {nextDayNumber} reward is ready to claim.";

            return new DailyCheckinRewardState(
                CurrentStreak: currentStreak,
                Subtitle: subtitle,
                Availability: claimedToday ? "claimed" : "available",
                Progress: Math.Min(1d, currentStreak / 7d),
                IsClaimAvailable: !claimedToday,
                NextAvailableAtUtc: claimedToday ? today.AddDays(1) : null,
                LastClaimDateUtc: lastClaimDateUtc == default ? null : lastClaimDateUtc,
                CoinsAwarded: policies.DailyCheckinCoins);
        }

        private static WatchAdRewardState BuildWatchAdState(
            int claimsToday,
            StorePremiumRewardPolicyOptions policies)
        {
            var remaining = Math.Max(0, policies.WatchAdDailyCap - claimsToday);
            var subtitle = remaining > 0
                ? $"{remaining} of {policies.WatchAdDailyCap} claims remaining today."
                : "Daily watch-ad claim cap reached for today.";

            return new WatchAdRewardState(
                RemainingClaims: remaining,
                Subtitle: subtitle,
                Availability: remaining > 0 ? "available" : "claimed",
                Progress: policies.WatchAdDailyCap <= 0 ? 0 : (double)claimsToday / policies.WatchAdDailyCap,
                IsClaimAvailable: remaining > 0,
                NextAvailableAtUtc: null,
                CoinsAwarded: policies.WatchAdCoins);
        }

        private static int CountConsecutiveDays(IReadOnlyList<DateTimeOffset> claimDaysUtc)
        {
            if (claimDaysUtc.Count == 0)
                return 0;

            var streak = 1;
            var previous = claimDaysUtc[0];
            for (var i = 1; i < claimDaysUtc.Count; i++)
            {
                if (claimDaysUtc[i] == previous.AddDays(-1))
                {
                    streak++;
                    previous = claimDaysUtc[i];
                    continue;
                }

                break;
            }

            return streak;
        }

        private static DateTimeOffset StartOfUtcDay(DateTimeOffset value)
            => new(value.UtcDateTime.Date, TimeSpan.Zero);

        private static string BuildDefaultRewardSubtitle(string rewardId)
        {
            return rewardId switch
            {
                "daily-checkin" => "Claim once per UTC day.",
                "watch-ad" => "Claim up to the daily cap.",
                _ => "Reward details unavailable."
            };
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

        private static async Task<IResult?> EnsureStoreEnabledAsync(
            IAppDb db,
            IConfiguration configuration,
            CancellationToken ct)
        {
            var status = await StoreSystemStatusSupport.GetStatusAsync(db, configuration, ct);
            if (status.StoreEnabled)
                return null;

            return ApiResponses.Error(
                StatusCodes.Status503ServiceUnavailable,
                "STORE_DISABLED",
                "Store transactions are currently disabled.",
                status);
        }

        private static async Task<IResult?> EnsurePaymentsEnabledAsync(
            IAppDb db,
            IConfiguration configuration,
            CancellationToken ct)
        {
            var flags = await StoreSystemStatusSupport.LoadFlagsAsync(db, ct);
            var purchasesEnabled = flags.GetValueOrDefault(StoreSystemStatusSupport.StorePurchasesEnabledFlag, false);
            if (!purchasesEnabled)
                return Results.Json(
                    new { error = new { code = "FeatureDisabled", message = "Store purchases are not available in the current release.", details = new { } } },
                    statusCode: StatusCodes.Status403Forbidden);

            var status = await StoreSystemStatusSupport.GetStatusAsync(db, configuration, ct);
            if (!status.StoreEnabled)
            {
                return ApiResponses.Error(
                    StatusCodes.Status503ServiceUnavailable,
                    "STORE_DISABLED",
                    "Store transactions are currently disabled.",
                    status);
            }

            if (status.PaymentsEnabled)
                return null;

            return ApiResponses.Error(
                StatusCodes.Status503ServiceUnavailable,
                "PAYMENTS_DISABLED",
                "External payment flows are currently disabled.",
                status);
        }

        private static async Task<IResult?> EnsureStripeEnabledAsync(
            IAppDb db,
            IConfiguration configuration,
            CancellationToken ct)
        {
            var paymentsGate = await EnsurePaymentsEnabledAsync(db, configuration, ct);
            if (paymentsGate is not null)
                return paymentsGate;

            var status = await StoreSystemStatusSupport.GetStatusAsync(db, configuration, ct);
            if (status.StripeEnabled)
                return null;

            return ApiResponses.Error(
                StatusCodes.Status503ServiceUnavailable,
                "STRIPE_DISABLED",
                "Stripe payments are currently unavailable.",
                status);
        }

        private static async Task<IResult?> EnsurePayPalEnabledAsync(
            IAppDb db,
            IConfiguration configuration,
            CancellationToken ct)
        {
            var paymentsGate = await EnsurePaymentsEnabledAsync(db, configuration, ct);
            if (paymentsGate is not null)
                return paymentsGate;

            var status = await StoreSystemStatusSupport.GetStatusAsync(db, configuration, ct);
            if (status.PayPalEnabled)
                return null;

            return ApiResponses.Error(
                StatusCodes.Status503ServiceUnavailable,
                "PAYPAL_DISABLED",
                "PayPal payments are currently unavailable.",
                status);
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

        private const string DailyCheckinTransactionKind = "store-reward-daily-checkin";

        private const string WatchAdTransactionKind = "store-reward-watch-ad";

        private sealed record RewardClaimHistoryRow(string Kind, DateTimeOffset OccurredAtUtc);

        private sealed record DailyCheckinRewardState(
            int CurrentStreak,
            string Subtitle,
            string Availability,
            double Progress,
            bool IsClaimAvailable,
            DateTimeOffset? NextAvailableAtUtc,
            DateTimeOffset? LastClaimDateUtc,
            int CoinsAwarded);

        private sealed record WatchAdRewardState(
            int RemainingClaims,
            string Subtitle,
            string Availability,
            double Progress,
            bool IsClaimAvailable,
            DateTimeOffset? NextAvailableAtUtc,
            int CoinsAwarded);

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
