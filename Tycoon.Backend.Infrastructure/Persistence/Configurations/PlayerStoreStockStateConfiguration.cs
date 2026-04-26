using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PlayerStoreStockStateConfiguration : IEntityTypeConfiguration<PlayerStoreStockState>
    {
        public void Configure(EntityTypeBuilder<PlayerStoreStockState> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.PlayerId, x.Sku }).IsUnique();
            builder.HasIndex(x => x.PlayerId);

            builder.Property(x => x.PlayerId).IsRequired();
            builder.Property(x => x.Sku).HasMaxLength(128).IsRequired();
            builder.Property(x => x.QuantityUsed).IsRequired();
            builder.Property(x => x.EffectiveMaxQuantity).IsRequired(false);
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
        }
    }
}
