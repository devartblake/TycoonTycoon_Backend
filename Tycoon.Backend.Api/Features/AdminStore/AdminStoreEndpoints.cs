using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Api.Features.Store;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminStore;

public static class AdminStoreEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/store").WithTags("Admin/Store");

        // Catalog + system status (existing)
        g.MapGet("/system/status", GetSystemStatus);
        g.MapPatch("/system/status", UpdateSystemStatus);
        g.MapGet("/catalog", ListCatalog);
        g.MapPost("/catalog", CreateItem);
        g.MapPatch("/catalog/{id:guid}", UpdateItem);
        g.MapDelete("/catalog/{id:guid}", DeleteItem);

        // P2 — Stock policies
        g.MapGet("/stock-policies", ListStockPolicies);
        g.MapPut("/stock-policies/{sku}", UpsertStockPolicy);
        g.MapPost("/stock-policies/bulk-reset", BulkResetStock);

        // P2 — Player stock
        g.MapGet("/player-stock/{playerId:guid}", GetPlayerStock);
        g.MapPost("/player-stock/{playerId:guid}/override", OverridePlayerStock);

        // P2 — Flash sales
        g.MapGet("/flash-sales", ListFlashSales);
        g.MapPost("/flash-sales", CreateFlashSale);
        g.MapPut("/flash-sales/{id:guid}", UpdateFlashSale);
        g.MapDelete("/flash-sales/{id:guid}", CancelFlashSale);

        // P2 — Delete stock policy
        g.MapDelete("/stock-policies/{sku}", DeleteStockPolicy);

        // P2 — Reward claim limits
        g.MapGet("/reward-limits", ListRewardLimits);
        g.MapGet("/reward-limits/{rewardId}", GetRewardLimit);
        g.MapPut("/reward-limits/{rewardId}", UpsertRewardLimit);

        // P2 — Analytics
        g.MapGet("/analytics/purchases", GetPurchaseAnalytics);
        g.MapGet("/analytics/stock-resets", GetStockResetAnalytics);
    }

    // -------------------------------------------------------------------------
    // Existing handlers
    // -------------------------------------------------------------------------

    private static async Task<IResult> GetSystemStatus(
        IAppDb db, IConfiguration configuration, CancellationToken ct)
    {
        var status = await StoreSystemStatusSupport.GetStatusAsync(db, configuration, ct);
        return Results.Ok(status);
    }

    private static async Task<IResult> UpdateSystemStatus(
        [FromBody] UpdateStoreSystemStatusRequest request,
        IAppDb db, IConfiguration configuration, CancellationToken ct)
    {
        var config = await StoreSystemStatusSupport.GetOrCreateConfigAsync(db, ct);
        Dictionary<string, bool> flags;
        try { flags = JsonSerializer.Deserialize<Dictionary<string, bool>>(config.FeatureFlagsJson) ?? []; }
        catch { flags = []; }

        StoreSystemStatusSupport.ApplyUpdate(flags, request);
        config.Update(enableLogging: null, featureFlagsJson: JsonSerializer.Serialize(flags));
        await db.SaveChangesAsync(ct);

        var status = await StoreSystemStatusSupport.GetStatusAsync(db, configuration, ct);
        return Results.Ok(new UpdateStoreSystemStatusResponse(status, config.UpdatedAt));
    }

    private static async Task<IResult> ListCatalog(
        [FromQuery] bool? activeOnly, [FromQuery] string? itemType,
        [FromQuery] int page, [FromQuery] int pageSize,
        IAppDb db, CancellationToken ct)
    {
        page = page <= 0 ? 1 : page;
        pageSize = Math.Clamp(pageSize <= 0 ? 25 : pageSize, 1, 200);
        var q = db.StoreItems.AsNoTracking();
        if (activeOnly == true) q = q.Where(i => i.IsActive);
        if (!string.IsNullOrWhiteSpace(itemType)) q = q.Where(i => i.ItemType == itemType);
        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new AdminStoreItemDto(i.Id, i.Sku, i.Name, i.Description, i.ItemType,
                i.PriceCoins, i.PriceDiamonds, i.GrantQuantity, i.MaxPerPlayer,
                i.IsActive, i.MediaKey, i.SortOrder, i.CreatedAtUtc, i.UpdatedAtUtc))
            .ToListAsync(ct);
        return Results.Ok(AdminApiResponses.Page(items, page, pageSize, total));
    }

    private static async Task<IResult> CreateItem(
        [FromBody] AdminCreateStoreItemRequest req, IAppDb db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Sku) || string.IsNullOrWhiteSpace(req.Name))
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "sku and name are required.");
        var sku = req.Sku.Trim().ToLowerInvariant();
        if (await db.StoreItems.AnyAsync(i => i.Sku == sku, ct))
            return AdminApiResponses.Error(409, "SKU_CONFLICT", $"A store item with SKU '{sku}' already exists.");
        if (req.PriceCoins < 0 || req.PriceDiamonds < 0)
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "Prices cannot be negative.");
        if (req.GrantQuantity <= 0)
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "grantQuantity must be at least 1.");
        var item = new StoreItem
        {
            Sku = sku, Name = req.Name.Trim(),
            Description = req.Description?.Trim() ?? string.Empty,
            ItemType = req.ItemType?.Trim().ToLowerInvariant() ?? "misc",
            PriceCoins = req.PriceCoins, PriceDiamonds = req.PriceDiamonds,
            GrantQuantity = req.GrantQuantity, MaxPerPlayer = Math.Max(0, req.MaxPerPlayer),
            MediaKey = req.MediaKey?.Trim(), SortOrder = req.SortOrder, IsActive = true,
        };
        db.StoreItems.Add(item);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/admin/store/catalog/{item.Id}", new { id = item.Id, sku = item.Sku });
    }

    private static async Task<IResult> UpdateItem(
        [FromRoute] Guid id, [FromBody] AdminUpdateStoreItemRequest req,
        IAppDb db, CancellationToken ct)
    {
        if (id == Guid.Empty)
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "id is required.");
        var item = await db.StoreItems.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null)
            return AdminApiResponses.Error(404, "NOT_FOUND", "Store item not found.");
        if (req.Name is not null) item.Name = req.Name.Trim();
        if (req.Description is not null) item.Description = req.Description.Trim();
        if (req.PriceCoins is { } coins)
        {
            if (coins < 0) return AdminApiResponses.Error(400, "VALIDATION_ERROR", "priceCoins cannot be negative.");
            item.PriceCoins = coins;
        }
        if (req.PriceDiamonds is { } diamonds)
        {
            if (diamonds < 0) return AdminApiResponses.Error(400, "VALIDATION_ERROR", "priceDiamonds cannot be negative.");
            item.PriceDiamonds = diamonds;
        }
        if (req.IsActive is { } active) item.IsActive = active;
        if (req.MediaKey is not null) item.MediaKey = req.MediaKey.Trim();
        if (req.SortOrder is { } order) item.SortOrder = order;
        item.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = item.Id, updatedAt = item.UpdatedAtUtc });
    }

    private static async Task<IResult> DeleteItem(
        [FromRoute] Guid id, IAppDb db, CancellationToken ct)
    {
        if (id == Guid.Empty)
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "id is required.");
        var item = await db.StoreItems.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null)
            return AdminApiResponses.Error(404, "NOT_FOUND", "Store item not found.");
        if (!item.IsActive)
            return AdminApiResponses.Error(409, "ALREADY_INACTIVE", "Store item is already inactive.");
        item.IsActive = false;
        item.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // -------------------------------------------------------------------------
    // P2 — Stock policies
    // -------------------------------------------------------------------------

    private static async Task<IResult> ListStockPolicies(
        [FromQuery] bool? activeOnly, [FromQuery] string? sku,
        IAppDb db, CancellationToken ct)
    {
        var q = db.StoreStockPolicies.AsNoTracking();
        if (activeOnly == true) q = q.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(sku)) q = q.Where(p => p.Sku == sku.Trim().ToLowerInvariant());
        var policies = await q.OrderBy(p => p.Sku)
            .Select(p => new AdminStockPolicyDto(p.Sku, p.MaxQuantityPerUser, p.ResetInterval,
                p.IsActive, p.CreatedAtUtc, p.UpdatedAtUtc))
            .ToListAsync(ct);
        return Results.Ok(new { policies });
    }

    private static async Task<IResult> UpsertStockPolicy(
        [FromRoute] string sku, [FromBody] AdminUpsertStockPolicyRequest req,
        IAppDb db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "sku is required.");
        if (req.MaxQuantityPerUser < 0)
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "maxQuantityPerUser must be >= 0 (0 = unlimited).");
        if (!new[] { "daily", "weekly", "monthly", "none" }.Contains(req.ResetInterval?.ToLowerInvariant()))
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "resetInterval must be 'daily', 'weekly', 'monthly', or 'none'.");

        var normalizedSku = sku.Trim().ToLowerInvariant();
        var policy = await db.StoreStockPolicies.FirstOrDefaultAsync(p => p.Sku == normalizedSku, ct);

        if (policy is null)
        {
            policy = new StoreStockPolicy(normalizedSku, req.MaxQuantityPerUser, req.ResetInterval!.ToLowerInvariant());
            if (req.IsActive == false) policy.Update(req.MaxQuantityPerUser, req.ResetInterval!.ToLowerInvariant(), false);
            db.StoreStockPolicies.Add(policy);
        }
        else
        {
            policy.Update(req.MaxQuantityPerUser, req.ResetInterval!.ToLowerInvariant(), req.IsActive);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new AdminStockPolicyDto(policy.Sku, policy.MaxQuantityPerUser,
            policy.ResetInterval, policy.IsActive, policy.CreatedAtUtc, policy.UpdatedAtUtc));
    }

    private static async Task<IResult> BulkResetStock(
        [FromBody] AdminBulkResetRequest req, IAppDb db, CancellationToken ct)
    {
        if (req.Skus is null || req.Skus.Count == 0)
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "skus must contain at least one entry.");

        var normalizedSkus = req.Skus.Select(s => s.Trim().ToLowerInvariant()).Distinct().ToList();
        var policies = await db.StoreStockPolicies
            .Where(p => normalizedSkus.Contains(p.Sku))
            .ToDictionaryAsync(p => p.Sku, ct);
        var states = await db.PlayerStoreStockStates
            .Where(s => normalizedSkus.Contains(s.Sku))
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var state in states)
        {
            if (policies.TryGetValue(state.Sku, out var policy))
                state.BulkReset(policy, now);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { skusReset = normalizedSkus, playersAffected = states.Count, resetAt = now, reason = req.Reason });
    }

    // -------------------------------------------------------------------------
    // P2 — Player stock
    // -------------------------------------------------------------------------

    private static async Task<IResult> GetPlayerStock(
        [FromRoute] Guid playerId, IAppDb db, CancellationToken ct)
    {
        var states = await db.PlayerStoreStockStates
            .AsNoTracking().Where(s => s.PlayerId == playerId).OrderBy(s => s.Sku).ToListAsync(ct);
        var policies = await db.StoreStockPolicies
            .AsNoTracking().Where(p => states.Select(s => s.Sku).Contains(p.Sku))
            .ToDictionaryAsync(p => p.Sku, ct);

        var items = states.Select(s =>
        {
            policies.TryGetValue(s.Sku, out var pol);
            var maxQty = s.EffectiveMaxQuantity ?? pol?.MaxQuantityPerUser ?? 0;
            var remaining = pol is null
                ? (s.EffectiveMaxQuantity.HasValue ? Math.Max(0, s.EffectiveMaxQuantity.Value - s.QuantityUsed) : -1)
                : s.GetRemaining(pol);
            return new AdminPlayerStockStateDto(s.Sku, s.QuantityUsed, maxQty, remaining,
                s.EffectiveMaxQuantity, s.LastResetAtUtc, s.NextResetAtUtc, s.UpdatedAtUtc);
        }).ToList();

        return Results.Ok(new { playerId, items });
    }

    private static async Task<IResult> OverridePlayerStock(
        [FromRoute] Guid playerId, [FromBody] AdminPlayerStockOverrideRequest req,
        IAppDb db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Sku))
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "sku is required.");
        if (req.EffectiveMaxQuantity is < 0)
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "effectiveMaxQuantity must be >= 0, or null to clear the override.");

        var normalizedSku = req.Sku.Trim().ToLowerInvariant();
        var state = await db.PlayerStoreStockStates
            .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.Sku == normalizedSku, ct);

        if (state is null)
        {
            var policy = await db.StoreStockPolicies
                .FirstOrDefaultAsync(p => p.Sku == normalizedSku && p.IsActive, ct);
            if (policy is null)
                return AdminApiResponses.Error(404, "NOT_FOUND", $"No stock policy found for SKU '{normalizedSku}'.");
            state = PlayerStoreStockState.Create(playerId, normalizedSku, policy);
            db.PlayerStoreStockStates.Add(state);
        }

        state.SetOverride(req.EffectiveMaxQuantity);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            playerId, sku = normalizedSku,
            effectiveMaxQuantity = state.EffectiveMaxQuantity,
            updatedAt = state.UpdatedAtUtc,
            reason = req.Reason
        });
    }

    // -------------------------------------------------------------------------
    // P2 — Flash sales
    // -------------------------------------------------------------------------

    private static async Task<IResult> ListFlashSales(
        [FromQuery] bool? showAll, IAppDb db, CancellationToken ct)
    {
        var query = db.FlashSales.AsNoTracking().AsQueryable();
        if (showAll != true)
        {
            var now = DateTimeOffset.UtcNow;
            query = query.Where(f => f.IsActive && f.EndsAtUtc >= now);
        }
        var sales = await query.OrderBy(f => f.StartsAtUtc)
            .Select(f => new AdminFlashSaleDto(f.Id, f.Sku, f.DiscountPercent,
                f.StartsAtUtc, f.EndsAtUtc, f.IsActive, f.Reason, f.CreatedAtUtc))
            .ToListAsync(ct);
        return Results.Ok(new { sales });
    }

    private static async Task<IResult> CreateFlashSale(
        [FromBody] AdminCreateFlashSaleRequest req, IAppDb db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Sku))
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "sku is required.");
        if (req.DiscountPercent is < 1 or > 99)
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "discountPercent must be between 1 and 99.");
        if (req.StartsAtUtc >= req.EndsAtUtc)
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "startsAtUtc must be before endsAtUtc.");

        var sku = req.Sku.Trim().ToLowerInvariant();
        if (!await db.StoreItems.AnyAsync(i => i.Sku == sku && i.IsActive, ct))
            return AdminApiResponses.Error(404, "NOT_FOUND", $"No active store item found for SKU '{sku}'.");

        var overlapping = await db.FlashSales.AnyAsync(f =>
            f.Sku == sku && f.IsActive &&
            f.StartsAtUtc < req.EndsAtUtc && f.EndsAtUtc > req.StartsAtUtc, ct);
        if (overlapping)
            return AdminApiResponses.Error(409, "SALE_OVERLAP",
                $"An active flash sale for '{sku}' already overlaps the requested window.");

        var sale = new FlashSale(sku, req.DiscountPercent, req.StartsAtUtc, req.EndsAtUtc, req.Reason?.Trim());
        db.FlashSales.Add(sale);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/admin/store/flash-sales/{sale.Id}",
            new AdminFlashSaleDto(sale.Id, sale.Sku, sale.DiscountPercent,
                sale.StartsAtUtc, sale.EndsAtUtc, sale.IsActive, sale.Reason, sale.CreatedAtUtc));
    }

    private static async Task<IResult> CancelFlashSale(
        [FromRoute] Guid id, IAppDb db, CancellationToken ct)
    {
        var sale = await db.FlashSales.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (sale is null)
            return AdminApiResponses.Error(404, "NOT_FOUND", "Flash sale not found.");
        if (!sale.IsActive)
            return AdminApiResponses.Error(409, "ALREADY_CANCELLED", "Flash sale is already cancelled.");
        sale.Cancel();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteStockPolicy(
        [FromRoute] string sku, IAppDb db, CancellationToken ct)
    {
        var normalized = sku.ToLowerInvariant();
        var policy = await db.StoreStockPolicies.FirstOrDefaultAsync(p => p.Sku == normalized, ct);
        if (policy is null)
            return AdminApiResponses.Error(404, "NOT_FOUND", $"No policy found for SKU '{normalized}'.");
        db.StoreStockPolicies.Remove(policy);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> UpdateFlashSale(
        [FromRoute] Guid id, [FromBody] AdminUpdateFlashSaleRequest req, IAppDb db, CancellationToken ct)
    {
        var sale = await db.FlashSales.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (sale is null)
            return AdminApiResponses.Error(404, "NOT_FOUND", "Flash sale not found.");
        if (!sale.IsActive)
            return AdminApiResponses.Error(409, "ALREADY_CANCELLED", "Cannot edit a cancelled sale.");
        if (sale.StartsAtUtc <= DateTimeOffset.UtcNow)
            return AdminApiResponses.Error(409, "ALREADY_STARTED", "Cannot edit a sale that has already started.");
        if (req.DiscountPercent is < 1 or > 99)
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "discountPercent must be 1–99.");
        if (req.EndsAtUtc <= req.StartsAtUtc)
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "endsAtUtc must be after startsAtUtc.");
        sale.Update(req.DiscountPercent, req.StartsAtUtc, req.EndsAtUtc, req.Reason);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new AdminFlashSaleDto(sale.Id, sale.Sku, sale.DiscountPercent,
            sale.StartsAtUtc, sale.EndsAtUtc, sale.IsActive, sale.Reason, sale.CreatedAtUtc));
    }

    // -------------------------------------------------------------------------
    // P2 — Reward claim limits
    // -------------------------------------------------------------------------

    private static async Task<IResult> ListRewardLimits(IAppDb db, CancellationToken ct)
    {
        var rules = await db.RewardClaimRules.AsNoTracking()
            .OrderBy(r => r.RewardId)
            .Select(r => new AdminRewardLimitDto(r.RewardId, r.MaxClaimsPerInterval, r.ResetInterval, r.IsActive, r.UpdatedAtUtc))
            .ToListAsync(ct);
        return Results.Ok(new { items = rules, total = rules.Count });
    }

    private static async Task<IResult> GetRewardLimit(
        [FromRoute] string rewardId, IAppDb db, CancellationToken ct)
    {
        var normalizedId = rewardId.Trim().ToLowerInvariant();
        var rule = await db.RewardClaimRules.AsNoTracking()
            .FirstOrDefaultAsync(r => r.RewardId == normalizedId, ct);
        if (rule is null)
            return AdminApiResponses.Error(404, "NOT_FOUND", $"No reward claim rule found for rewardId '{normalizedId}'.");
        return Results.Ok(new AdminRewardLimitDto(rule.RewardId, rule.MaxClaimsPerInterval,
            rule.ResetInterval, rule.IsActive, rule.UpdatedAtUtc));
    }

    private static async Task<IResult> UpsertRewardLimit(
        [FromRoute] string rewardId, [FromBody] AdminUpsertRewardLimitRequest req,
        IAppDb db, CancellationToken ct)
    {
        if (req.MaxClaimsPerInterval < 1)
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "maxClaimsPerInterval must be >= 1.");
        if (!new[] { "daily", "weekly", "none" }.Contains(req.ResetInterval?.ToLowerInvariant()))
            return AdminApiResponses.Error(400, "VALIDATION_ERROR", "resetInterval must be 'daily', 'weekly', or 'none'.");

        var normalizedId = rewardId.Trim().ToLowerInvariant();
        var rule = await db.RewardClaimRules.FirstOrDefaultAsync(r => r.RewardId == normalizedId, ct);

        if (rule is null)
        {
            rule = RewardClaimRule.Create(normalizedId, req.MaxClaimsPerInterval, req.ResetInterval!.ToLowerInvariant());
            db.RewardClaimRules.Add(rule);
        }
        else
        {
            rule.Update(req.MaxClaimsPerInterval, req.ResetInterval!.ToLowerInvariant(), req.IsActive ?? rule.IsActive);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new AdminRewardLimitDto(rule.RewardId, rule.MaxClaimsPerInterval,
            rule.ResetInterval, rule.IsActive, rule.UpdatedAtUtc));
    }

    // -------------------------------------------------------------------------
    // P2 — Analytics
    // -------------------------------------------------------------------------

    private static async Task<IResult> GetPurchaseAnalytics(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] string? sku,
        IAppDb db, CancellationToken ct)
    {
        var q = db.PlayerTransactions.AsNoTracking()
            .Where(t => t.Kind == "store-purchase" && t.Status == PlayerTransactionStatus.Applied);
        if (from.HasValue) q = q.Where(t => t.CreatedAtUtc >= from.Value);
        if (to.HasValue)   q = q.Where(t => t.CreatedAtUtc <= to.Value);

        var transactions = await q
            .Select(t => new
            {
                Items = t.ItemChanges.Select(i => new { i.ItemType, i.Quantity, i.Operation })
            })
            .ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(sku))
        {
            var normalizedSku = sku.Trim().ToLowerInvariant();
            transactions = transactions
                .Where(t => t.Items.Any(i => i.ItemType == normalizedSku && i.Operation == ItemOperation.Grant))
                .ToList();
        }

        var totalPurchases = transactions.Count;
        var totalCoinsSpent = transactions
            .SelectMany(t => t.Items)
            .Where(i => i.ItemType == "coins" && i.Quantity < 0)
            .Sum(i => Math.Abs(i.Quantity));

        var topSkus = transactions
            .SelectMany(t => t.Items)
            .Where(i => i.Operation == ItemOperation.Grant && i.ItemType != "coins" && i.ItemType != "xp")
            .GroupBy(i => i.ItemType)
            .Select(g => new { sku = g.Key, purchaseCount = g.Count() })
            .OrderByDescending(x => x.purchaseCount)
            .Take(10)
            .ToList();

        return Results.Ok(new { from, to, totalPurchases, totalCoinsSpent, topSkus });
    }

    private static async Task<IResult> GetStockResetAnalytics(
        [FromQuery] string? sku, [FromQuery] int page, [FromQuery] int pageSize,
        IAppDb db, CancellationToken ct)
    {
        page = page <= 0 ? 1 : page;
        pageSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 200);

        var q = db.PlayerStoreStockStates.AsNoTracking().Where(s => s.LastResetAtUtc != null);
        if (!string.IsNullOrWhiteSpace(sku))
            q = q.Where(s => s.Sku == sku.Trim().ToLowerInvariant());

        var total = await q.CountAsync(ct);
        var resets = await q
            .OrderByDescending(s => s.LastResetAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new
            {
                s.PlayerId, s.Sku,
                lastResetAt = s.LastResetAtUtc,
                nextResetAt = s.NextResetAtUtc,
                quantityUsed = s.QuantityUsed
            })
            .ToListAsync(ct);

        return Results.Ok(AdminApiResponses.Page(resets, page, pageSize, total));
    }

    // -------------------------------------------------------------------------
    // DTOs + request records
    // -------------------------------------------------------------------------

    public sealed record AdminStoreItemDto(
        Guid Id, string Sku, string Name, string Description, string ItemType,
        int PriceCoins, int PriceDiamonds, int GrantQuantity, int MaxPerPlayer,
        bool IsActive, string? MediaKey, int SortOrder,
        DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

    public sealed record AdminCreateStoreItemRequest(
        string Sku, string Name, string? Description, string? ItemType,
        int PriceCoins, int PriceDiamonds, int GrantQuantity, int MaxPerPlayer,
        string? MediaKey, int SortOrder);

    public sealed record AdminUpdateStoreItemRequest(
        string? Name, string? Description, int? PriceCoins, int? PriceDiamonds,
        bool? IsActive, string? MediaKey, int? SortOrder);

    public sealed record AdminStockPolicyDto(
        string Sku, int MaxQuantityPerUser, string ResetInterval, bool IsActive,
        DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

    public sealed record AdminUpsertStockPolicyRequest(
        int MaxQuantityPerUser, string? ResetInterval, bool? IsActive);

    public sealed record AdminBulkResetRequest(List<string> Skus, string? Reason);

    public sealed record AdminPlayerStockStateDto(
        string Sku, int QuantityUsed, int MaxQuantity, int Remaining,
        int? EffectiveMaxQuantity,
        DateTimeOffset? LastResetAtUtc, DateTimeOffset? NextResetAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public sealed record AdminPlayerStockOverrideRequest(
        string Sku, int? EffectiveMaxQuantity, string? Reason);

    public sealed record AdminFlashSaleDto(
        Guid Id, string Sku, int DiscountPercent,
        DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc,
        bool IsActive, string? Reason, DateTimeOffset CreatedAtUtc);

    public sealed record AdminCreateFlashSaleRequest(
        string Sku, int DiscountPercent,
        DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc,
        string? Reason);

    public sealed record AdminUpdateFlashSaleRequest(
        int DiscountPercent, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string? Reason);

    public sealed record AdminRewardLimitDto(
        string RewardId, int MaxClaimsPerInterval, string ResetInterval,
        bool IsActive, DateTimeOffset UpdatedAtUtc);

    public sealed record AdminUpsertRewardLimitRequest(
        int MaxClaimsPerInterval, string? ResetInterval, bool? IsActive);
}
