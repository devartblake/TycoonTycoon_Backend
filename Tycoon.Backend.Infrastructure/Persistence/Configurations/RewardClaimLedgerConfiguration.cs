using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class RewardClaimLedgerConfiguration : IEntityTypeConfiguration<RewardClaimLedger>
{
    public void Configure(EntityTypeBuilder<RewardClaimLedger> builder)
    {
        builder.ToTable("reward_claim_ledger");

        builder.HasKey(x => x.Id);

        // Uniqueness: one claim per spinId per player
        builder.HasIndex(x => new { x.PlayerId, x.SpinId }).IsUnique();

        // Idempotency: one ledger entry per claim idempotency key per player
        builder.HasIndex(x => new { x.PlayerId, x.IdempotencyKey }).IsUnique();

        // Audit and query indexes
        builder.HasIndex(x => new { x.PlayerId, x.Mechanism, x.AppliedAtUtc });

        builder.Property(x => x.PlayerId).IsRequired();
        builder.Property(x => x.Mechanism).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.SpinId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RewardId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RewardLinesJson).IsRequired();
        builder.Property(x => x.ClaimStatus).HasMaxLength(32).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AppliedAtUtc).IsRequired();
        builder.Property(x => x.AuditCorrelationId).IsRequired();
    }
}
