namespace Synaptix.Backend.Api.Payments.Stripe;

public sealed class StripeOptions
{
    public bool Enabled { get; set; }

    public string? SecretKey { get; set; }

    public string? PublishableKey { get; set; }

    public string? WebhookSecret { get; set; }

    public string? SuccessUrl { get; set; }

    public string? CancelUrl { get; set; }

    public string? PortalReturnUrl { get; set; }

    public bool AllowPromotionCodes { get; set; } = true;

    public bool AutomaticTax { get; set; }

    public string DefaultCurrency { get; set; } = "usd";

    public List<StripeCatalogItemOptions> Catalog { get; set; } = [];

    public List<StripeSubscriptionPlanOptions> SubscriptionPlans { get; set; } = [];
}

public sealed class StripeCatalogItemOptions
{
    public string Sku { get; set; } = string.Empty;

    public long UnitAmount { get; set; }

    public string? Currency { get; set; }

    public string? ProductName { get; set; }

    public string? ProductDescription { get; set; }
}

public sealed class StripeSubscriptionPlanOptions
{
    public string Tier { get; set; } = string.Empty;

    public string BillingPeriod { get; set; } = string.Empty;

    public string PriceId { get; set; } = string.Empty;

    public string? ProductName { get; set; }
}
