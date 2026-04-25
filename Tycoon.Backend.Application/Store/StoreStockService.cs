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
    }
}
