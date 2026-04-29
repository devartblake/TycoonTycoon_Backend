using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Experiments;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class ExperimentVariantConfiguration : IEntityTypeConfiguration<ExperimentVariant>
{
    public void Configure(EntityTypeBuilder<ExperimentVariant> b)
    {
        b.ToTable("experiment_variants");
        b.HasKey(x => x.Id);

        b.Property(x => x.ExperimentId).HasColumnName("experiment_id").IsRequired();
        b.Property(x => x.Key).HasColumnName("key").HasMaxLength(128).IsRequired();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        b.Property(x => x.Weight).HasColumnName("weight").HasPrecision(6, 2).HasDefaultValue(50m);
        b.Property(x => x.IsControl).HasColumnName("is_control").HasDefaultValue(false);
        b.Property(x => x.ConfigJson).HasColumnName("config_json").HasColumnType("jsonb").IsRequired().HasDefaultValue("{}");

        b.HasIndex(x => new { x.ExperimentId, x.Key }).IsUnique();
    }
}
