using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Entitlements.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class PlayerEntitlementConfiguration : IEntityTypeConfiguration<PlayerEntitlement>
{
    public void Configure(EntityTypeBuilder<PlayerEntitlement> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.PlayerId, x.Sku, x.Scope });
        builder.HasIndex(x => x.SourceTransactionId);
        builder.Property(x => x.Sku).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ItemType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Scope).HasMaxLength(32).IsRequired().HasDefaultValue("permanent");
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.GrantedAtUtc).IsRequired();
    }
}
