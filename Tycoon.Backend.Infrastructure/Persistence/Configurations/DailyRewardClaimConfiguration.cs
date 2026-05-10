using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class DailyRewardClaimConfiguration : IEntityTypeConfiguration<DailyRewardClaim>
{
    public void Configure(EntityTypeBuilder<DailyRewardClaim> builder)
    {
        builder.ToTable("daily_reward_claims");
        builder.HasKey(x => x.Id);
        // One claim per player per day
        builder.HasIndex(x => new { x.PlayerId, x.ClaimDate }).IsUnique();
        builder.Property(x => x.PlayerId).IsRequired();
        builder.Property(x => x.ClaimDate).IsRequired();
        builder.Property(x => x.CoinsGranted).IsRequired();
        builder.Property(x => x.ClaimedAtUtc).IsRequired();
    }
}
