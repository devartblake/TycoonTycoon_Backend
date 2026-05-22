using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class StoreStockPolicyConfiguration : IEntityTypeConfiguration<StoreStockPolicy>
    {
        public void Configure(EntityTypeBuilder<StoreStockPolicy> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Sku).IsUnique();

            builder.Property(x => x.Sku).HasMaxLength(128).IsRequired();
            builder.Property(x => x.MaxQuantityPerUser).IsRequired();
            builder.Property(x => x.ResetInterval).HasMaxLength(20).IsRequired();
            builder.Property(x => x.IsActive).HasDefaultValue(true);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
        }
    }
}
