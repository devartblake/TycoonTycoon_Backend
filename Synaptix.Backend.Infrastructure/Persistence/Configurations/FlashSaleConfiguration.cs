using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class FlashSaleConfiguration : IEntityTypeConfiguration<FlashSale>
    {
        public void Configure(EntityTypeBuilder<FlashSale> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.Sku, x.StartsAtUtc, x.EndsAtUtc })
                .HasDatabaseName("ix_flash_sales_sku_window");
            builder.HasIndex(x => x.EndsAtUtc)
                .HasDatabaseName("ix_flash_sales_ends_at_utc");

            builder.Property(x => x.Sku).HasMaxLength(128).IsRequired();
            builder.Property(x => x.DiscountPercent).IsRequired();
            builder.Property(x => x.StartsAtUtc).IsRequired();
            builder.Property(x => x.EndsAtUtc).IsRequired();
            builder.Property(x => x.IsActive).HasDefaultValue(true);
            builder.Property(x => x.Reason).HasMaxLength(256);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
        }
    }
}
