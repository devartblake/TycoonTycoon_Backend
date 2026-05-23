using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class RewardChainTicketConfiguration : IEntityTypeConfiguration<RewardChainTicket>
{
    public void Configure(EntityTypeBuilder<RewardChainTicket> builder)
    {
        builder.ToTable("reward_chain_tickets");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ChainedSpinId).IsUnique();
        builder.HasIndex(x => new { x.PlayerId, x.SourceSpinId }).IsUnique();
        builder.HasIndex(x => new { x.PlayerId, x.Status, x.ExpiresAtUtc });

        builder.Property(x => x.ChainedSpinId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PlayerId).IsRequired();
        builder.Property(x => x.SourceSpinId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RewardId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RewardLinesJson).IsRequired();
        builder.Property(x => x.AnimationJson).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.ActivatedAtUtc);
        builder.Property(x => x.GeneratedSpinId).HasMaxLength(64);
        builder.Property(x => x.GeneratedClaimToken).HasMaxLength(200);
    }
}
