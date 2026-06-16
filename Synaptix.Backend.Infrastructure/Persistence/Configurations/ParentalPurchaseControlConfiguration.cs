using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class ParentalPurchaseControlConfiguration : IEntityTypeConfiguration<ParentalPurchaseControl>
{
    public void Configure(EntityTypeBuilder<ParentalPurchaseControl> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ChildUserId).IsUnique();
        builder.Property(x => x.MonthlySpendLimitCents).HasDefaultValue(0);
        builder.Property(x => x.PurchasesEnabled).HasDefaultValue(false);
        builder.Property(x => x.AdsEnabled).HasDefaultValue(true);
        builder.Property(x => x.LootBoxesEnabled).HasDefaultValue(false);
    }
}
