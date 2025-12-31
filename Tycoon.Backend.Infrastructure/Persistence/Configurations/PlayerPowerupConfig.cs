using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PlayerPowerupConfig : IEntityTypeConfiguration<PlayerPowerup>
    {
        public void Configure(EntityTypeBuilder<PlayerPowerup> b)
        {
            b.ToTable("player_powerups");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.Type).IsRequired();

            b.HasIndex(x => new { x.PlayerId, x.Type }).IsUnique();

            b.Property(x => x.Quantity).IsRequired();
            b.Property(x => x.CooldownUntilUtc);
            b.Property(x => x.UpdatedAtUtc).IsRequired();
        }
    }
}
