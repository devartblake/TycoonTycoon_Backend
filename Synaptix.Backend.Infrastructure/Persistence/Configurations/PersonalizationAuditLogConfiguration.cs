using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Personalization;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class PersonalizationAuditLogConfiguration : IEntityTypeConfiguration<PersonalizationAuditLog>
{
    public void Configure(EntityTypeBuilder<PersonalizationAuditLog> b)
    {
        b.ToTable("personalization_audit_logs");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.PlayerId).HasColumnName("player_id");
        b.Property(x => x.RecommendationId).HasColumnName("recommendation_id");

        b.Property(x => x.DecisionType).HasColumnName("decision_type").HasMaxLength(128);
        b.Property(x => x.Source).HasColumnName("source").HasMaxLength(64).HasDefaultValue("backend");
        b.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(512).HasDefaultValue("");

        b.Property(x => x.InputSignalsJson).HasColumnName("input_signals_json").HasColumnType("jsonb").HasDefaultValue("{}");
        b.Property(x => x.CandidateJson).HasColumnName("candidate_json").HasColumnType("jsonb").HasDefaultValue("{}");
        b.Property(x => x.GuardrailsAppliedJson).HasColumnName("guardrails_applied_json").HasColumnType("jsonb").HasDefaultValue("{}");
        b.Property(x => x.FinalDecisionJson).HasColumnName("final_decision_json").HasColumnType("jsonb").HasDefaultValue("{}");

        b.Property(x => x.CreatedAt).HasColumnName("created_at");

        b.HasIndex(x => new { x.PlayerId, x.CreatedAt }).HasDatabaseName("ix_personalization_audit_logs_player_created");
        b.HasIndex(x => x.DecisionType).HasDatabaseName("ix_personalization_audit_logs_decision_type");
        b.HasIndex(x => x.RecommendationId).HasDatabaseName("ix_personalization_audit_logs_recommendation_id");
    }
}
