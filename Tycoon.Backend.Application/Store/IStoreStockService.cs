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

        /// <summary>
        /// Returns the full store catalog resolved for a specific player, including per-player stock
        /// state, ownership, availability, and any active flash-sale discounts.
        /// Optionally filtered by <paramref name="itemType"/> or <paramref name="category"/> prefix.
        /// </summary>
        Task<PlayerStoreCatalogResponseDto> GetCatalogForPlayerAsync(
            Guid playerId, string? itemType, string? category, CancellationToken ct);

        /// <summary>
        /// Returns the store hub surface: featured items (enriched), daily stock items, and a
        /// deduplicated list of item-type categories drawn from the active catalog.
        /// </summary>
        Task<StoreHubResponseDto> GetHubAsync(Guid playerId, CancellationToken ct);

        /// <summary>
        /// Returns currently active flash sales joined with the matching catalog items.
        /// </summary>
        Task<IReadOnlyList<SpecialOfferDto>> GetSpecialOffersAsync(CancellationToken ct);
    }
}
