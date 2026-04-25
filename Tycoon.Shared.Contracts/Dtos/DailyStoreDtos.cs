namespace Tycoon.Shared.Contracts.Dtos
{
    public sealed record DailyStoreItemDto(
        string Sku,
        string Name,
        string Description,
        string ItemType,
        int PriceCoins,
        int PriceDiamonds,
        int RemainingQuantity,
        int MaxQuantity,
        string ResetInterval,
        bool SoldOut,
        int DiscountPercent,
        DateTimeOffset? NextResetAt
    );

    public sealed record DailyStoreResponseDto(
        DateTimeOffset GeneratedAt,
        DateTimeOffset ResetsAt,
        IReadOnlyList<DailyStoreItemDto> Items
    );
}
