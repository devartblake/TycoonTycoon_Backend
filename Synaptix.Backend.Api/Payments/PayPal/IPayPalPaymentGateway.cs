namespace Synaptix.Backend.Api.Payments.PayPal;

public interface IPayPalPaymentGateway
{
    Task<bool> VerifyWebhookAsync(
        PayPalWebhookVerificationRequest request,
        CancellationToken cancellationToken);

    Task<PayPalCreateOrderResult> CreateOrderAsync(
        PayPalCreateOrderRequest request,
        CancellationToken cancellationToken);

    Task<PayPalCaptureOrderResult> CaptureOrderAsync(
        string orderId,
        CancellationToken cancellationToken);

    Task<PayPalCreateSubscriptionResult> CreateSubscriptionAsync(
        PayPalCreateSubscriptionRequest request,
        CancellationToken cancellationToken);

    Task<PayPalSubscriptionDetails> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken);

    Task CancelSubscriptionAsync(
        string subscriptionId,
        string reason,
        CancellationToken cancellationToken);
}

public sealed record PayPalWebhookVerificationRequest(
    string TransmissionId,
    string TransmissionTime,
    string TransmissionSig,
    string CertUrl,
    string AuthAlgo,
    string WebhookId,
    string WebhookEventJson);

public sealed record PayPalCreateOrderRequest(
    Guid PlayerId,
    string Sku,
    string Name,
    string Description,
    int Quantity,
    decimal UnitAmount,
    string Currency,
    string ReturnUrl,
    string CancelUrl);

public sealed record PayPalCreateOrderResult(
    string OrderId,
    string Status,
    string? ApproveUrl);

public sealed record PayPalCaptureOrderResult(
    string OrderId,
    string Status,
    string? CaptureId,
    string? CustomId,
    string? Currency,
    decimal? TotalAmount);

public sealed record PayPalCreateSubscriptionRequest(
    Guid PlayerId,
    string? PlayerEmail,
    string Tier,
    string BillingPeriod,
    string PlanId,
    string ReturnUrl,
    string CancelUrl);

public sealed record PayPalCreateSubscriptionResult(
    string SubscriptionId,
    string Status,
    string? ApproveUrl);

public sealed record PayPalSubscriptionDetails(
    string SubscriptionId,
    string Status,
    string? PlanId,
    string? CustomId,
    DateTimeOffset? StatusUpdateTimeUtc,
    DateTimeOffset? NextBillingTimeUtc,
    string? SubscriberEmail);
