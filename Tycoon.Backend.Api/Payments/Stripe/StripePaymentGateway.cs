using Microsoft.Extensions.Options;
using Stripe;

namespace Tycoon.Backend.Api.Payments.Stripe;

public sealed class StripePaymentGateway : IStripePaymentGateway
{
    private readonly StripeOptions _options;

    public StripePaymentGateway(IOptions<StripeOptions> options)
    {
        _options = options.Value;
    }

    public async Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(
        StripeCheckoutSessionCreateRequest request,
        CancellationToken cancellationToken)
    {
        EnsureStripeConfigured();

        StripeConfiguration.ApiKey = _options.SecretKey;

        var service = new global::Stripe.Checkout.SessionService();
        var session = await service.CreateAsync(
            new global::Stripe.Checkout.SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                ClientReferenceId = request.PlayerId.ToString(),
                CustomerEmail = request.PlayerEmail,
                AllowPromotionCodes = _options.AllowPromotionCodes,
                AutomaticTax = new global::Stripe.Checkout.SessionAutomaticTaxOptions
                {
                    Enabled = _options.AutomaticTax
                },
                LineItems =
                [
                    new global::Stripe.Checkout.SessionLineItemOptions
                    {
                        Quantity = request.Quantity,
                        PriceData = new global::Stripe.Checkout.SessionLineItemPriceDataOptions
                        {
                            Currency = request.Currency,
                            UnitAmount = request.UnitAmount,
                            ProductData = new global::Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                            {
                                Name = request.Name,
                                Description = request.Description,
                                Metadata = new Dictionary<string, string>
                                {
                                    ["sku"] = request.Sku
                                }
                            }
                        }
                    }
                ],
                Metadata = request.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            },
            requestOptions: null,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Id) || string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe did not return a checkout session URL.");
        }

        return new StripeCheckoutSessionResult(session.Id, session.Url);
    }

    public async Task<StripeCheckoutSessionResult> CreateSubscriptionCheckoutSessionAsync(
        StripeSubscriptionCheckoutSessionCreateRequest request,
        CancellationToken cancellationToken)
    {
        EnsureStripeConfigured();

        StripeConfiguration.ApiKey = _options.SecretKey;

        var service = new global::Stripe.Checkout.SessionService();
        var session = await service.CreateAsync(
            new global::Stripe.Checkout.SessionCreateOptions
            {
                Mode = "subscription",
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                ClientReferenceId = request.PlayerId.ToString(),
                CustomerEmail = request.PlayerEmail,
                AllowPromotionCodes = _options.AllowPromotionCodes,
                AutomaticTax = new global::Stripe.Checkout.SessionAutomaticTaxOptions
                {
                    Enabled = _options.AutomaticTax
                },
                LineItems =
                [
                    new global::Stripe.Checkout.SessionLineItemOptions
                    {
                        Quantity = 1,
                        Price = request.PriceId
                    }
                ],
                Metadata = request.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                SubscriptionData = new global::Stripe.Checkout.SessionSubscriptionDataOptions
                {
                    Metadata = request.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                }
            },
            requestOptions: null,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Id) || string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe did not return a subscription checkout session URL.");
        }

        return new StripeCheckoutSessionResult(session.Id, session.Url);
    }

    public async Task<StripeBillingPortalSessionResult> CreateBillingPortalSessionAsync(
        StripeBillingPortalSessionCreateRequest request,
        CancellationToken cancellationToken)
    {
        EnsureStripeConfigured();

        StripeConfiguration.ApiKey = _options.SecretKey;

        var service = new global::Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(
            new global::Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = request.CustomerId,
                ReturnUrl = request.ReturnUrl
            },
            requestOptions: null,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Id) || string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe did not return a billing portal URL.");
        }

        return new StripeBillingPortalSessionResult(session.Id, session.Url);
    }

    public StripeWebhookEvent ParseWebhook(string payload, string? signatureHeader)
    {
        EnsureStripeConfigured();

        Event stripeEvent;
        if (!string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            if (string.IsNullOrWhiteSpace(signatureHeader))
            {
                throw new InvalidOperationException("Stripe-Signature header is required.");
            }

            stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _options.WebhookSecret);
        }
        else
        {
            stripeEvent = EventUtility.ParseEvent(payload);
        }

        if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted
            && stripeEvent.Data.Object is global::Stripe.Checkout.Session session)
        {
            return new StripeWebhookEvent(
                stripeEvent.Id,
                stripeEvent.Type,
                new StripeWebhookCheckoutCompletedData(
                    session.Id,
                    session.Mode,
                    session.PaymentStatus,
                    session.Currency,
                    session.AmountTotal,
                    session.CustomerDetails?.Email ?? session.CustomerEmail,
                    session.ClientReferenceId,
                    session.SubscriptionId,
                    session.CustomerId,
                    session.Metadata ?? new Dictionary<string, string>()),
                null);
        }

        if ((stripeEvent.Type == EventTypes.CustomerSubscriptionUpdated
            || stripeEvent.Type == EventTypes.CustomerSubscriptionDeleted)
            && stripeEvent.Data.Object is global::Stripe.Subscription subscription)
        {
            return new StripeWebhookEvent(
                stripeEvent.Id,
                stripeEvent.Type,
                null,
                new StripeWebhookSubscriptionData(
                    subscription.Id,
                    subscription.CustomerId,
                    subscription.Status,
                    subscription.CancelAtPeriodEnd,
                    // Stripe.net v48: CurrentPeriodEnd moved from Subscription to SubscriptionItem.
                    subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd is { } periodEnd
                        ? new DateTimeOffset(periodEnd, TimeSpan.Zero)
                        : (DateTimeOffset?)null,
                    subscription.Metadata ?? new Dictionary<string, string>()));
        }

        return new StripeWebhookEvent(stripeEvent.Id, stripeEvent.Type, null, null);
    }

    private void EnsureStripeConfigured()
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("Stripe payments are disabled.");
        }

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new InvalidOperationException("Stripe:SecretKey must be configured.");
        }
    }
}
