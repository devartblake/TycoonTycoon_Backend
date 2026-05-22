using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Experiments;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class ExperimentAssignmentConfiguration : IEntityTypeConfiguration<ExperimentAssignment>
{
    public void Configure(EntityTypeBuilder<ExperimentAssignment> b)
    {
        b.ToTable("experiment_assignments");
        b.HasKey(x => x.Id);

        b.Property(x => x.PlayerId).HasColumnName("player_id").IsRequired();
        b.Property(x => x.ExperimentId).HasColumnName("experiment_id").IsRequired();
        b.Property(x => x.ExperimentKey).HasColumnName("experiment_key").HasMaxLength(128).IsRequired();
        b.Property(x => x.VariantKey).HasColumnName("variant_key").HasMaxLength(128).IsRequired();
        b.Property(x => x.AssignedAt).HasColumnName("assigned_at").IsRequired();
        b.Property(x => x.FirstSeenAt).HasColumnName("first_seen_at").IsRequired(false);
        b.Property(x => x.ImpressionCount).HasColumnName("impression_count").HasDefaultValue(0);
        b.Property(x => x.OutcomeCount).HasColumnName("outcome_count").HasDefaultValue(0);
        b.Property(x => x.OutcomeJson).HasColumnName("outcome_json").HasColumnType("jsonb").IsRequired().HasDefaultValue("{}");

        // One assignment per player per experiment
        b.HasIndex(x => new { x.PlayerId, x.ExperimentId }).IsUnique();
        // Fast lookup by experiment for analytics
        b.HasIndex(x => new { x.ExperimentId, x.VariantKey });
    }
}
