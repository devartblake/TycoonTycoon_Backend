using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class SeasonRewardRuleConfiguration : IEntityTypeConfiguration<SeasonRewardRule>
    {
        public void Configure(EntityTypeBuilder<SeasonRewardRule> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.Tier, x.MaxTierRank }).IsUnique();

            builder.Property(x => x.Tier).IsRequired();
            builder.Property(x => x.MaxTierRank).IsRequired();
            builder.Property(x => x.RewardXp).IsRequired();
            builder.Property(x => x.RewardCoins).IsRequired();
        }
    }
}
