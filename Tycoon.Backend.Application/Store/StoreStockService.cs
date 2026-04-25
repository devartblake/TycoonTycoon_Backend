using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Store
{
    public sealed class StoreStockService : IStoreStockService
    {
        private readonly IAppDb _db;

        public StoreStockService(IAppDb db) => _db = db;

        // ── P0: check + consume ───────────────────────────────────────────────

        public async Task<string?> CheckStockAsync(Guid playerId, string sku, int quantity, CancellationToken ct)
        {
            var policy = await _db.StoreStockPolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Sku == sku && p.IsActive, ct);

            if (policy is null || policy.MaxQuantityPerUser == 0) return null;

            var state = await _db.PlayerStoreStockStates
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.Sku == sku, ct);

            var remaining = state is null
                ? policy.MaxQuantityPerUser
                : state.NextResetAtUtc <= DateTimeOffset.UtcNow
                    ? policy.MaxQuantityPerUser
                    : state.GetRemaining(policy);

            return remaining < quantity ? "store_item_out_of_stock" : null;
        }

        public async Task ConsumeStockAsync(Guid playerId, string sku, int quantity, CancellationToken ct)
        {
            var policy = await _db.StoreStockPolicies
                .FirstOrDefaultAsync(p => p.Sku == sku && p.IsActive, ct);

            if (policy is null || policy.MaxQuantityPerUser == 0) return;

            var state = await _db.PlayerStoreStockStates
                .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.Sku == sku, ct);

            if (state is null)
            {
                state = PlayerStoreStockState.Create(playerId, sku, policy);
                _db.PlayerStoreStockStates.Add(state);
            }
            else
            {
                state.ResetIfExpired(policy);
            }

            state.Consume(quantity);
            await _db.SaveChangesAsync(ct);
        }

        // ── P0: daily store ───────────────────────────────────────────────────

        public async Task<IReadOnlyList<DailyStoreItemDto>> GetDailyItemsAsync(Guid playerId, CancellationToken ct)
        {
            var policies = await _db.StoreStockPolicies
                .AsNoTracking()
                .Where(p => p.IsActive)
                .ToListAsync(ct);

            if (policies.Count == 0) return Array.Empty<DailyStoreItemDto>();

            var skus = policies.Select(p => p.Sku).ToHashSet();

            var items = await _db.StoreItems
                .AsNoTracking()
                .Where(i => i.IsActive && skus.Contains(i.Sku))
                .OrderBy(i => i.SortOrder)
                .ToListAsync(ct);

            if (items.Count == 0) return Array.Empty<DailyStoreItemDto>();

            var itemSkus = items.Select(i => i.Sku).ToList();
            var stockStates = await _db.PlayerStoreStockStates
                .AsNoTracking()
                .Where(s => s.PlayerId == playerId && itemSkus.Contains(s.Sku))
                .ToDictionaryAsync(s => s.Sku, ct);

            var policyMap = policies.ToDictionary(p => p.Sku);
            var now = DateTimeOffset.UtcNow;

            return items.Select(item =>
            {
                var policy = policyMap.GetValueOrDefault(item.Sku);
                stockStates.TryGetValue(item.Sku, out var state);

                int remaining;
                DateTimeOffset? nextResetAt = null;

                if (policy is null || policy.MaxQuantityPerUser == 0)
                {
                    remaining = -1;
                }
                else if (state is null)
                {
                    remaining = policy.MaxQuantityPerUser;
                    nextResetAt = policy.CalculateNextReset(now);
                }
                else
                {
                    var expired = state.NextResetAtUtc <= now;
                    remaining = expired ? policy.MaxQuantityPerUser : state.GetRemaining(policy);
                    nextResetAt = expired ? policy.CalculateNextReset(now) : state.NextResetAtUtc;
                }

                return new DailyStoreItemDto(
                    Sku: item.Sku,
                    Name: item.Name,
                    Description: item.Description,
                    ItemType: item.ItemType,
                    PriceCoins: item.PriceCoins,
                    PriceDiamonds: item.PriceDiamonds,
                    RemainingQuantity: remaining,
                    MaxQuantity: policy?.MaxQuantityPerUser ?? -1,
                    ResetInterval: policy?.ResetInterval ?? "none",
                    SoldOut: remaining == 0,
                    DiscountPercent: 0,
                    NextResetAt: nextResetAt
                );
            }).ToList();
        }

        // ── P1: player catalog ────────────────────────────────────────────────

        public async Task<PlayerStoreCatalogResponseDto> GetCatalogForPlayerAsync(
            Guid playerId, string? itemType, string? category, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            var query = _db.StoreItems.AsNoTracking().Where(i => i.IsActive);

            if (!string.IsNullOrWhiteSpace(itemType))
                query = query.Where(i => i.ItemType == itemType);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(i => i.ItemType == category || i.ItemType.StartsWith(category + ":"));

            var items = await query.OrderBy(i => i.SortOrder).ToListAsync(ct);

            if (items.Count == 0)
                return new PlayerStoreCatalogResponseDto(playerId, now, Array.Empty<PlayerStoreCatalogItemDto>());

            var skus = items.Select(i => i.Sku).ToList();

            var (policies, stockStates, activeSales, ownedSkus) = await LoadPlayerCatalogDataAsync(playerId, skus, now, ct);

            var result = items.Select(item =>
                MapToCatalogItem(item, policies, stockStates, activeSales, ownedSkus, now)
            ).ToList();

            return new PlayerStoreCatalogResponseDto(playerId, now, result);
        }

        // ── P1: hub ───────────────────────────────────────────────────────────

        public async Task<StoreHubResponseDto> GetHubAsync(Guid playerId, CancellationToken ct)
        {
            var catalog = await GetCatalogForPlayerAsync(playerId, null, null, ct);
            var daily   = await GetDailyItemsAsync(playerId, ct);

            var featured   = catalog.Items.Where(i => i.IsFeatured).ToList();
            var categories = catalog.Items.Select(i => i.ItemType).Distinct().OrderBy(x => x).ToList();

            return new StoreHubResponseDto(featured, daily, categories);
        }

        // ── P1: special offers ────────────────────────────────────────────────

        public async Task<IReadOnlyList<SpecialOfferDto>> GetSpecialOffersAsync(CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            var sales = await _db.FlashSales
                .AsNoTracking()
                .Where(f => f.IsActive && f.StartsAtUtc <= now && f.EndsAtUtc >= now)
                .OrderBy(f => f.EndsAtUtc)
                .ToListAsync(ct);

            if (sales.Count == 0) return Array.Empty<SpecialOfferDto>();

            var skus = sales.Select(f => f.Sku).ToHashSet();
            var itemMap = await _db.StoreItems
                .AsNoTracking()
                .Where(i => i.IsActive && skus.Contains(i.Sku))
                .ToDictionaryAsync(i => i.Sku, ct);

            return sales
                .Where(f => itemMap.ContainsKey(f.Sku))
                .Select(f =>
                {
                    var item = itemMap[f.Sku];
                    var salePrice = item.PriceCoins - item.PriceCoins * f.DiscountPercent / 100;
                    return new SpecialOfferDto(
                        Sku: item.Sku,
                        Name: item.Name,
                        Description: item.Description,
                        OriginalPriceCoins: item.PriceCoins,
                        SalePriceCoins: salePrice,
                        DiscountPercent: f.DiscountPercent,
                        EndsAt: f.EndsAtUtc
                    );
                })
                .ToList();
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private async Task<(
            Dictionary<string, StoreStockPolicy> policies,
            Dictionary<string, PlayerStoreStockState> stockStates,
            Dictionary<string, FlashSale> activeSales,
            HashSet<string> ownedSkus)>
            LoadPlayerCatalogDataAsync(Guid playerId, List<string> skus, DateTimeOffset now, CancellationToken ct)
        {
            var policiesTask = _db.StoreStockPolicies
                .AsNoTracking()
                .Where(p => p.IsActive && skus.Contains(p.Sku))
                .ToDictionaryAsync(p => p.Sku, ct);

            var stockTask = _db.PlayerStoreStockStates
                .AsNoTracking()
                .Where(s => s.PlayerId == playerId && skus.Contains(s.Sku))
                .ToDictionaryAsync(s => s.Sku, ct);

            var salesTask = _db.FlashSales
                .AsNoTracking()
                .Where(f => f.IsActive && f.StartsAtUtc <= now && f.EndsAtUtc >= now && skus.Contains(f.Sku))
                .ToDictionaryAsync(f => f.Sku, ct);

            var ownedTask = _db.PlayerTransactions
                .AsNoTracking()
                .Where(t => t.Kind == "store-purchase"
                            && t.Status == PlayerTransactionStatus.Applied
                            && t.Actors.Any(a => a.PlayerId == playerId))
                .SelectMany(t => t.ItemChanges)
                .Select(i => i.ItemType)
                .Distinct()
                .ToListAsync(ct);

            await Task.WhenAll(policiesTask, stockTask, salesTask, ownedTask);

            return (await policiesTask, await stockTask, await salesTask, (await ownedTask).ToHashSet());
        }

        private static PlayerStoreCatalogItemDto MapToCatalogItem(
            StoreItem item,
            Dictionary<string, StoreStockPolicy> policies,
            Dictionary<string, PlayerStoreStockState> stockStates,
            Dictionary<string, FlashSale> activeSales,
            HashSet<string> ownedSkus,
            DateTimeOffset now)
        {
            policies.TryGetValue(item.Sku, out var policy);
            stockStates.TryGetValue(item.Sku, out var state);
            activeSales.TryGetValue(item.Sku, out var sale);

            var owned = ownedSkus.Contains(item.Sku);

            int remaining;
            DateTimeOffset? nextResetAt = null;
            DateTimeOffset? lastResetAt = null;

            if (policy is null || policy.MaxQuantityPerUser == 0)
            {
                remaining = -1;
            }
            else if (state is null)
            {
                remaining = policy.MaxQuantityPerUser;
                nextResetAt = policy.CalculateNextReset(now);
            }
            else
            {
                var expired = state.NextResetAtUtc <= now;
                remaining = expired ? policy.MaxQuantityPerUser : state.GetRemaining(policy);
                nextResetAt = expired ? policy.CalculateNextReset(now) : state.NextResetAtUtc;
                lastResetAt = state.LastResetAtUtc;
            }

            var alreadyOwned = owned && item.MaxPerPlayer == 1;
            var soldOut = remaining == 0;

            var availabilityState = alreadyOwned ? "already_owned"
                : soldOut ? "sold_out"
                : "available";

            var stockState = remaining == -1 ? "unlimited"
                : remaining == 0 ? "out_of_stock"
                : remaining == 1 ? "low_stock"
                : "in_stock";

            return new PlayerStoreCatalogItemDto(
                Sku: item.Sku,
                Name: item.Name,
                Description: item.Description,
                ItemType: item.ItemType,
                PriceCoins: item.PriceCoins,
                PriceDiamonds: item.PriceDiamonds,
                IsAvailable: availabilityState == "available",
                RemainingQuantity: remaining,
                MaxQuantity: policy?.MaxQuantityPerUser ?? -1,
                ResetInterval: policy?.ResetInterval,
                LastResetAt: lastResetAt,
                NextResetAt: nextResetAt,
                SoldOut: soldOut,
                DiscountPercent: sale?.DiscountPercent ?? 0,
                Owned: owned,
                AvailabilityState: availabilityState,
                StockState: stockState,
                ThumbnailUrl: item.ThumbnailUrl,
                IsFeatured: item.IsFeatured
            );
        }
    }
}
