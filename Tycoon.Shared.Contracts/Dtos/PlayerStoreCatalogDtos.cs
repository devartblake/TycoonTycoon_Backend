namespace Tycoon.Shared.Contracts.Dtos
{
    public sealed record PlayerStoreCatalogItemDto(
        string Sku,
        string Name,
        string? Description,
        string ItemType,
        int PriceCoins,
        int PriceDiamonds,
        bool IsAvailable,
        int RemainingQuantity,
        int MaxQuantity,
        string? ResetInterval,
        DateTimeOffset? LastResetAt,
        DateTimeOffset? NextResetAt,
        bool SoldOut,
        int DiscountPercent,
        bool Owned,
        string AvailabilityState,
        string StockState,
        string? ThumbnailUrl,
        bool IsFeatured
    );

    public sealed record PlayerStoreCatalogResponseDto(
        Guid PlayerId,
        DateTimeOffset GeneratedAt,
        IReadOnlyList<PlayerStoreCatalogItemDto> Items
    );

    public sealed record StoreHubResponseDto(
        IReadOnlyList<PlayerStoreCatalogItemDto> Featured,
        IReadOnlyList<DailyStoreItemDto> Daily,
        IReadOnlyList<string> Categories
    );

    public sealed record SpecialOfferDto(
        string Sku,
        string Name,
        string? Description,
        int OriginalPriceCoins,
        int SalePriceCoins,
        int DiscountPercent,
        DateTimeOffset EndsAt
    );

    public sealed record SpecialOffersResponseDto(
        IReadOnlyList<SpecialOfferDto> Offers
    );
}
