namespace Synaptix.Backend.Domain.Personalization;

public sealed class PersonalizationRecommendation
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }

    public string RecommendationType { get; set; } = "";
    public string Source { get; set; } = "backend";

    public int Priority { get; set; }
    public decimal Score { get; set; } = 0.50m;
    public string Reason { get; set; } = "";

    public string PayloadJson { get; set; } = "{}";
    public string GuardrailJson { get; set; } = "{}";

    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? DismissedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
