using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class SeasonRewardClaimConfiguration : IEntityTypeConfiguration<SeasonRewardClaim>
{
    public void Configure(EntityTypeBuilder<SeasonRewardClaim> b)
    {
        b.ToTable("season_reward_claims");
        b.HasKey(x => x.Id);

        b.Property(x => x.SeasonId).IsRequired();
        b.Property(x => x.PlayerId).IsRequired();
        b.Property(x => x.EventId).IsRequired();
        b.Property(x => x.RewardDay).IsRequired();
        b.Property(x => x.AwardedCoins).IsRequired();
        b.Property(x => x.AwardedXp).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();

        // idempotency
        b.HasIndex(x => x.EventId).IsUnique();

        // one claim per player per day per season
        b.HasIndex(x => new { x.SeasonId, x.PlayerId, x.RewardDay }).IsUnique();
    }
}
