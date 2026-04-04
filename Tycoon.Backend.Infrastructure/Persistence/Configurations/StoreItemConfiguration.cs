using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class StoreItemConfiguration : IEntityTypeConfiguration<StoreItem>
    {
        public void Configure(EntityTypeBuilder<StoreItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Sku).IsUnique();

            builder.Property(x => x.Sku).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(1024);
            builder.Property(x => x.ItemType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.MediaKey).HasMaxLength(512);
        }
    }
}
