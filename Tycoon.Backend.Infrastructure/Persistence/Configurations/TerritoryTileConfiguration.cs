using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class TerritoryTileConfiguration : IEntityTypeConfiguration<TerritoryTile>
    {
        public void Configure(EntityTypeBuilder<TerritoryTile> b)
        {
            b.ToTable("territory_tiles");
            b.HasKey(x => x.Id);
            b.Property(x => x.SeasonId).IsRequired();
            b.Property(x => x.TierNumber).IsRequired();
            b.Property(x => x.Category).HasMaxLength(64).IsRequired();
            b.Property(x => x.OwnerId);
            b.Property(x => x.CapturedAtUtc);
            b.Property(x => x.XpMultiplierBps).IsRequired().HasDefaultValue(0);
            b.HasIndex(x => new { x.SeasonId, x.TierNumber, x.Category }).IsUnique();
            b.HasIndex(x => x.OwnerId);
            b.Ignore(x => x.DomainEvents);
        }
    }
}
