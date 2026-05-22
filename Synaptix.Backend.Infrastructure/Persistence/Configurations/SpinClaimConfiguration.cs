using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class SpinClaimConfiguration : IEntityTypeConfiguration<SpinClaim>
{
    public void Configure(EntityTypeBuilder<SpinClaim> builder)
    {
        builder.ToTable("arcade_spin_claims");

        builder.HasKey(x => x.Id);

        // Idempotency — one spinId can only ever be claimed once
        builder.HasIndex(x => x.SpinId).IsUnique();

        // For per-player claim history queries
        builder.HasIndex(x => new { x.PlayerId, x.ClaimedAtUtc });

        builder.Property(x => x.PlayerId).IsRequired();
        builder.Property(x => x.SegmentId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SpinId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CoinsGranted).IsRequired();
        builder.Property(x => x.ClaimedAtUtc).IsRequired();
    }
}
