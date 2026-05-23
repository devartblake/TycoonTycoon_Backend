namespace Synaptix.Backend.Api.Features.Store
{
    public sealed class StorePremiumOptions
    {
        public int CacheMinutes { get; set; } = 15;

        public StorePremiumAdFreeOptions AdFree { get; set; } = new();

        public StorePremiumSaleOptions FlashSale { get; set; } = new();

        public StorePremiumRewardCenterOptions RewardCenter { get; set; } = new();

        public StorePremiumRewardPolicyOptions RewardPolicies { get; set; } = new();
    }

    public sealed class StorePremiumAdFreeOptions
    {
        public string Title { get; set; } = "Ad-Free Plans";

        public string Subtitle { get; set; } = "Play without interruption.";

        public List<string> Benefits { get; set; } = new();

        public List<StorePremiumPlanOptions> Plans { get; set; } = new();
    }

    public sealed class StorePremiumPlanOptions
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Subtitle { get; set; } = string.Empty;

        public string PriceLabel { get; set; } = string.Empty;

        public string? Badge { get; set; }

        public string AccentColor { get; set; } = "#1E88E5";

        public bool IsBestValue { get; set; }

        public string? Sku { get; set; }
    }

    public sealed class StorePremiumSaleOptions
    {
        public bool IsActive { get; set; }

        public string Badge { get; set; } = "Limited Time";

        public string Title { get; set; } = string.Empty;

        public string Subtitle { get; set; } = string.Empty;

        public string CtaLabel { get; set; } = "View Offer";

        public string GradientStart { get; set; } = "#FF8A65";

        public string GradientEnd { get; set; } = "#FF7043";

        public List<string> Benefits { get; set; } = new();
    }

    public sealed class StorePremiumRewardCenterOptions
    {
        public string Title { get; set; } = "Reward Center";

        public string Subtitle { get; set; } = "Collect your daily bonuses.";

        public List<StorePremiumRewardCardOptions> Cards { get; set; } = new();
    }

    public sealed class StorePremiumRewardCardOptions
    {
        public string RewardId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string RewardLabel { get; set; } = string.Empty;

        public string GradientStart { get; set; } = "#26A69A";

        public string GradientEnd { get; set; } = "#42A5F5";
    }

    public sealed class StorePremiumRewardPolicyOptions
    {
        public int DailyCheckinCoins { get; set; } = 25;

        public int WatchAdCoins { get; set; } = 15;

        public int WatchAdDailyCap { get; set; } = 3;
    }
}
