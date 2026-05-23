using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Configurations
{
    public class PlayerConfig : IEntityTypeConfiguration<Player>
    {
        public void Configure(EntityTypeBuilder<Player> e)
        {
            e.ToTable("players");
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(64).IsRequired();
            e.Property(x => x.CountryCode).HasMaxLength(4).IsRequired();
            e.Property(x => x.Level).IsRequired();
            e.Property(x => x.Xp).HasPrecision(12, 2);
            e.HasIndex(x => x.Username).IsUnique();
        }
    }
}
