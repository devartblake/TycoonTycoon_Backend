namespace Synaptix.Backend.Api.Payments.PayPal;

public sealed class PayPalOptions
{
    public bool Enabled { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string BaseUrl { get; set; } = "https://api-m.sandbox.paypal.com";

    public string? ReturnUrl { get; set; }

    public string? CancelUrl { get; set; }

    public string? BrandName { get; set; }

    public string? WebhookId { get; set; }

    /// <summary>
    /// Whether crypto is surfaced as a PayPal-hosted funding source ("PayPal Pay with Crypto").
    /// Requires PayPal merchant account eligibility/enrollment to actually take effect on
    /// PayPal's hosted checkout — this flag only controls whether Synaptix discloses the
    /// option to the client and does not itself alter custody, settlement (still USD), or
    /// the underlying Orders API call. Default false per the Alpha/Beta guardrail in
    /// docs/alpha-beta/SynaptixPlay_Managed_Crypto_Payments_Implementation_Plan.md §18.
    /// </summary>
    public bool AllowCryptoFundingSource { get; set; }

    public List<PayPalCatalogItemOptions> Catalog { get; set; } = [];

    public List<PayPalSubscriptionPlanOptions> SubscriptionPlans { get; set; } = [];
}

public sealed class PayPalCatalogItemOptions
{
    public string Sku { get; set; } = string.Empty;

    public string Currency { get; set; } = "USD";

    public decimal UnitAmount { get; set; }
}

public sealed class PayPalSubscriptionPlanOptions
{
    public string Tier { get; set; } = string.Empty;

    public string BillingPeriod { get; set; } = string.Empty;

    public string PlanId { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}
