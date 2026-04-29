using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Experiments;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class ExperimentConfiguration : IEntityTypeConfiguration<Experiment>
{
    public void Configure(EntityTypeBuilder<Experiment> b)
    {
        b.ToTable("experiments");
        b.HasKey(x => x.Id);

        b.Property(x => x.Key).HasColumnName("key").HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.Key).IsUnique();

        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000).IsRequired(false);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired().HasDefaultValue("draft");
        b.Property(x => x.AllocationPercent).HasColumnName("allocation_percent").HasPrecision(5, 2).HasDefaultValue(100m);
        b.Property(x => x.StartsAt).HasColumnName("starts_at").IsRequired(false);
        b.Property(x => x.EndsAt).HasColumnName("ends_at").IsRequired(false);
        b.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").IsRequired().HasDefaultValue("{}");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        b.HasIndex(x => x.Status);

        b.HasMany(x => x.Variants)
            .WithOne(v => v.Experiment)
            .HasForeignKey(v => v.ExperimentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
