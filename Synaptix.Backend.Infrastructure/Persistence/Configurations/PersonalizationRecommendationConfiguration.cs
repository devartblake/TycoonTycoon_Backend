using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Personalization;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class PersonalizationRecommendationConfiguration : IEntityTypeConfiguration<PersonalizationRecommendation>
{
    public void Configure(EntityTypeBuilder<PersonalizationRecommendation> b)
    {
        b.ToTable("personalization_recommendations");
        b.HasKey(x => x.Id);

        b.Property(x => x.PlayerId).HasColumnName("player_id");
        b.Property(x => x.RecommendationType).HasColumnName("recommendation_type").HasMaxLength(128);
        b.Property(x => x.Source).HasColumnName("source").HasMaxLength(64);
        b.Property(x => x.Priority).HasColumnName("priority");
        b.Property(x => x.Score).HasColumnName("score").HasPrecision(5, 2);
        b.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(512).HasDefaultValue("");
        b.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
        b.Property(x => x.GuardrailJson).HasColumnName("guardrail_json").HasColumnType("jsonb");
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        b.Property(x => x.AcceptedAt).HasColumnName("accepted_at");
        b.Property(x => x.DismissedAt).HasColumnName("dismissed_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");

        b.HasIndex(x => new { x.PlayerId, x.CreatedAt })
            .HasDatabaseName("ix_personalization_recommendations_player_created");
        b.HasIndex(x => x.RecommendationType)
            .HasDatabaseName("ix_personalization_recommendations_type");
    }
}
