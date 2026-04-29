namespace Tycoon.Backend.Domain.Personalization;

public sealed class PlayerMindProfile
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }

    public decimal ConfidenceLevel { get; set; } = 0.50m;
    public decimal RiskTolerance { get; set; } = 0.50m;

    public string PreferredPace { get; set; } = "balanced";
    public string LearningStyle { get; set; } = "mixed";
    public string CompetitivePreference { get; set; } = "balanced";
    public string SocialPreference { get; set; } = "solo";

    public decimal ChurnRiskScore { get; set; }
    public decimal FrustrationRiskScore { get; set; }
    public decimal RewardSensitivityScore { get; set; } = 0.50m;
    public decimal StoreAffinityScore { get; set; } = 0.50m;
    public decimal NotificationFatigueScore { get; set; }

    public string Archetype { get; set; } = "new_player";

    public string CategoryStrengthsJson { get; set; } = "{}";
    public string CategoryWeaknessesJson { get; set; } = "{}";
    public string PreferenceJson { get; set; } = "{}";
    public string GuardrailJson { get; set; } = "{}";
    public string SidecarScoresJson { get; set; } = "{}";

    public bool PersonalizationEnabled { get; set; } = true;
    public bool SidecarScoringEnabled { get; set; } = true;

    public DateTimeOffset? LastCalculatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
