using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Store
{
    public interface IStoreStockService
    {
        /// <summary>
        /// Checks whether the player has enough remaining stock for <paramref name="quantity"/> units.
        /// Returns null if the purchase is allowed; returns an error code string otherwise.
        /// Items with no stock policy are treated as unlimited and always return null.
        /// </summary>
        Task<string?> CheckStockAsync(Guid playerId, string sku, int quantity, CancellationToken ct);

        /// <summary>
        /// Records that the player consumed <paramref name="quantity"/> units.
        /// Creates the stock state row on first purchase and performs a lazy reset if the interval has expired.
        /// No-ops when no active stock policy exists for the SKU.
        /// </summary>
        Task ConsumeStockAsync(Guid playerId, string sku, int quantity, CancellationToken ct);

        /// <summary>
        /// Returns all active store items that have a stock policy, enriched with the
        /// calling player's current stock state (remaining quantity, reset time, sold-out flag).
        /// </summary>
        Task<IReadOnlyList<DailyStoreItemDto>> GetDailyItemsAsync(Guid playerId, CancellationToken ct);
    }
}
