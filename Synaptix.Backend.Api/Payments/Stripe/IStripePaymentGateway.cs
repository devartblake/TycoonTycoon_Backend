namespace Synaptix.Backend.Api.Payments.Stripe;

public interface IStripePaymentGateway
{
    Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(
        StripeCheckoutSessionCreateRequest request,
        CancellationToken cancellationToken);

    Task<StripeCheckoutSessionResult> CreateSubscriptionCheckoutSessionAsync(
        StripeSubscriptionCheckoutSessionCreateRequest request,
        CancellationToken cancellationToken);

    Task<StripeBillingPortalSessionResult> CreateBillingPortalSessionAsync(
        StripeBillingPortalSessionCreateRequest request,
        CancellationToken cancellationToken);

    StripeWebhookEvent ParseWebhook(string payload, string? signatureHeader);
}

public sealed record StripeCheckoutSessionCreateRequest(
    Guid PlayerId,
    string? PlayerEmail,
    string Sku,
    string Name,
    string Description,
    int Quantity,
    long UnitAmount,
    string Currency,
    string SuccessUrl,
    string CancelUrl,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record StripeCheckoutSessionResult(
    string SessionId,
    string CheckoutUrl);

public sealed record StripeSubscriptionCheckoutSessionCreateRequest(
    Guid PlayerId,
    string? PlayerEmail,
    string Tier,
    string BillingPeriod,
    string PriceId,
    string SuccessUrl,
    string CancelUrl,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record StripeBillingPortalSessionCreateRequest(
    string CustomerId,
    string ReturnUrl);

public sealed record StripeBillingPortalSessionResult(
    string SessionId,
    string Url);

public sealed record StripeWebhookEvent(
    string EventId,
    string EventType,
    StripeWebhookCheckoutCompletedData? CheckoutCompleted,
    StripeWebhookSubscriptionData? SubscriptionChanged = null);

public sealed record StripeWebhookCheckoutCompletedData(
    string SessionId,
    string? Mode,
    string? PaymentStatus,
    string? Currency,
    long? AmountTotal,
    string? CustomerEmail,
    string? ClientReferenceId,
    string? SubscriptionId,
    string? CustomerId,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record StripeWebhookSubscriptionData(
    string SubscriptionId,
    string? CustomerId,
    string? Status,
    bool CancelAtPeriodEnd,
    DateTimeOffset? CurrentPeriodEndUtc,
    IReadOnlyDictionary<string, string> Metadata);
