using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class RewardClaimRuleConfiguration : IEntityTypeConfiguration<RewardClaimRule>
    {
        public void Configure(EntityTypeBuilder<RewardClaimRule> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.RewardId).IsUnique().HasDatabaseName("ix_reward_claim_rules_reward_id");

            builder.Property(x => x.RewardId).HasMaxLength(128).IsRequired();
            builder.Property(x => x.MaxClaimsPerInterval).IsRequired();
            builder.Property(x => x.ResetInterval).HasMaxLength(20).IsRequired();
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
        }
    }
}
