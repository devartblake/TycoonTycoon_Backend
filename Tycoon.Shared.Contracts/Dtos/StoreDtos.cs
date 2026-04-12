namespace Tycoon.Shared.Contracts.Dtos
{
    public sealed record StoreItemDto(
        Guid Id,
        string Sku,
        string Name,
        string Description,
        string ItemType,
        int PriceCoins,
        int PriceDiamonds,
        int GrantQuantity,
        int MaxPerPlayer,
        string? MediaKey,
        int SortOrder);

    public sealed record StoreCatalogDto(
        IReadOnlyList<StoreItemDto> Items,
        int Count);

    public sealed record StorePurchaseRequest(
        Guid PlayerId,
        string Sku,
        int Quantity,
        string Currency);

    public sealed record CreateStripeCheckoutSessionRequest(
        Guid PlayerId,
        string Sku,
        int Quantity,
        string? SuccessUrl = null,
        string? CancelUrl = null);

    public sealed record CreateStripeCheckoutSessionResponse(
        string SessionId,
        string CheckoutUrl,
        string Currency,
        long UnitAmount,
        long TotalAmount,
        string Sku,
        int Quantity,
        string? PublishableKey);

    public sealed record CreatePayPalOrderRequest(
        Guid PlayerId,
        string Sku,
        int Quantity,
        string? ReturnUrl = null,
        string? CancelUrl = null);

    public sealed record CreatePayPalOrderResponse(
        string OrderId,
        string Status,
        string? ApproveUrl,
        string Currency,
        decimal UnitAmount,
        decimal TotalAmount,
        string Sku,
        int Quantity,
        string? ClientId);

    public sealed record CapturePayPalOrderRequest(
        Guid PlayerId,
        string OrderId);

    public sealed record CapturePayPalOrderResponse(
        string OrderId,
        string Status,
        string? CaptureId,
        Guid? TransactionId);

    public sealed record StorePurchaseResultDto(
        string Status,
        Guid? TransactionId,
        int BalanceXp,
        int BalanceCoins,
        int BalanceDiamonds,
        string? ErrorMessage);

    public sealed record PlayerInventoryItemDto(
        string ItemType,
        int Quantity);

    public sealed record PlayerInventoryDto(
        Guid PlayerId,
        IReadOnlyList<PlayerInventoryItemDto> Items,
        int Count);

    public sealed record ActivateSubscriptionRequest(
        Guid PlayerId,
        string Tier,
        string BillingPeriod,
        string? ExternalTransactionId = null);

    public sealed record CreateStripeSubscriptionCheckoutSessionRequest(
        Guid PlayerId,
        string Tier,
        string BillingPeriod,
        string? SuccessUrl = null,
        string? CancelUrl = null);

    public sealed record CreateStripeSubscriptionCheckoutSessionResponse(
        string SessionId,
        string CheckoutUrl,
        string PriceId,
        string Tier,
        string BillingPeriod,
        string? PublishableKey);

    public sealed record CreateStripeBillingPortalSessionRequest(
        Guid PlayerId,
        string? ReturnUrl = null);

    public sealed record CreateStripeBillingPortalSessionResponse(
        string SessionId,
        string Url);

    public sealed record CreatePayPalSubscriptionRequest(
        Guid PlayerId,
        string Tier,
        string BillingPeriod,
        string? ReturnUrl = null,
        string? CancelUrl = null);

    public sealed record CreatePayPalSubscriptionResponse(
        string SubscriptionId,
        string Status,
        string? ApproveUrl,
        string PlanId,
        string Tier,
        string BillingPeriod,
        string? ClientId);

    public sealed record CancelPayPalSubscriptionRequest(
        Guid PlayerId,
        string SubscriptionId,
        string? Reason = null);

    public sealed record SubscriptionStatusDto(
        Guid PlayerId,
        bool IsActive,
        string? Tier,
        string? BillingPeriod,
        DateTimeOffset? ActivatedAtUtc,
        string? Provider = null,
        string? ProviderSubscriptionId = null,
        string? ProviderCustomerId = null,
        string? ProviderStatus = null,
        string? StripeSubscriptionId = null,
        string? StripeCustomerId = null,
        string? StripeStatus = null,
        DateTimeOffset? CurrentPeriodEndUtc = null,
        bool CancelAtPeriodEnd = false);
}
