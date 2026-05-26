using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class PlayerLookupCodeConfiguration : IEntityTypeConfiguration<PlayerLookupCode>
{
    public void Configure(EntityTypeBuilder<PlayerLookupCode> builder)
    {
        builder.ToTable("player_lookup_codes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ShortCode)
            .IsRequired()
            .HasMaxLength(6);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.PlayerId).IsUnique();
        builder.HasIndex(x => x.ShortCode).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
