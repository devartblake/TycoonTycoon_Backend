using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class RewardSessionConfiguration : IEntityTypeConfiguration<RewardSession>
{
    public void Configure(EntityTypeBuilder<RewardSession> builder)
    {
        builder.ToTable("reward_sessions");

        builder.HasKey(x => x.Id);

        // Public spin ID — unique, used in all API calls
        builder.HasIndex(x => x.SpinId).IsUnique();

        // Idempotency: one start key per player
        builder.HasIndex(x => new { x.PlayerId, x.IdempotencyKey }).IsUnique();

        // Common queries
        builder.HasIndex(x => new { x.PlayerId, x.Mechanism, x.CreatedAtUtc });

        builder.Property(x => x.SpinId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PlayerId).IsRequired();
        builder.Property(x => x.Mechanism).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.RewardId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RewardLinesJson).IsRequired();
        builder.Property(x => x.AnimationJson).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ClaimTokenHash).HasMaxLength(256);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.ClaimedAtUtc);
        builder.Property(x => x.PolicySnapshotJson);
        builder.Property(x => x.ReactorId).HasMaxLength(100);
    }
}
