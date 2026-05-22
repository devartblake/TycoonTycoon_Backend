using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Personalization;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class PlayerMindProfileConfiguration : IEntityTypeConfiguration<PlayerMindProfile>
{
    public void Configure(EntityTypeBuilder<PlayerMindProfile> b)
    {
        b.ToTable("player_mind_profiles");
        b.HasKey(x => x.Id);

        b.Property(x => x.PlayerId).HasColumnName("player_id");
        b.HasIndex(x => x.PlayerId).IsUnique();

        b.Property(x => x.ConfidenceLevel).HasColumnName("confidence_level").HasPrecision(5, 2);
        b.Property(x => x.RiskTolerance).HasColumnName("risk_tolerance").HasPrecision(5, 2);
        b.Property(x => x.ChurnRiskScore).HasColumnName("churn_risk_score").HasPrecision(5, 2);
        b.Property(x => x.FrustrationRiskScore).HasColumnName("frustration_risk_score").HasPrecision(5, 2);
        b.Property(x => x.RewardSensitivityScore).HasColumnName("reward_sensitivity_score").HasPrecision(5, 2);
        b.Property(x => x.StoreAffinityScore).HasColumnName("store_affinity_score").HasPrecision(5, 2);
        b.Property(x => x.NotificationFatigueScore).HasColumnName("notification_fatigue_score").HasPrecision(5, 2);

        b.Property(x => x.PreferredPace).HasColumnName("preferred_pace").HasMaxLength(64);
        b.Property(x => x.LearningStyle).HasColumnName("learning_style").HasMaxLength(64);
        b.Property(x => x.CompetitivePreference).HasColumnName("competitive_preference").HasMaxLength(64);
        b.Property(x => x.SocialPreference).HasColumnName("social_preference").HasMaxLength(64);
        b.Property(x => x.Archetype).HasColumnName("archetype").HasMaxLength(96);

        b.Property(x => x.CategoryStrengthsJson).HasColumnName("category_strengths_json").HasColumnType("jsonb");
        b.Property(x => x.CategoryWeaknessesJson).HasColumnName("category_weaknesses_json").HasColumnType("jsonb");
        b.Property(x => x.PreferenceJson).HasColumnName("preference_json").HasColumnType("jsonb");
        b.Property(x => x.GuardrailJson).HasColumnName("guardrail_json").HasColumnType("jsonb");
        b.Property(x => x.SidecarScoresJson).HasColumnName("sidecar_scores_json").HasColumnType("jsonb");

        b.Property(x => x.PersonalizationEnabled).HasColumnName("personalization_enabled");
        b.Property(x => x.SidecarScoringEnabled).HasColumnName("sidecar_scoring_enabled");
        b.Property(x => x.LastCalculatedAt).HasColumnName("last_calculated_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(x => x.Archetype).HasDatabaseName("ix_player_mind_profiles_archetype");
        b.HasIndex(x => x.ChurnRiskScore).HasDatabaseName("ix_player_mind_profiles_churn_risk");
        b.HasIndex(x => x.FrustrationRiskScore).HasDatabaseName("ix_player_mind_profiles_frustration_risk");
        b.HasIndex(x => x.UpdatedAt).HasDatabaseName("ix_player_mind_profiles_updated_at");
    }
}
