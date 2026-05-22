using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class PlayerEconomySafeguardStateConfiguration : IEntityTypeConfiguration<PlayerEconomySafeguardState>
{
    public void Configure(EntityTypeBuilder<PlayerEconomySafeguardState> b)
    {
        b.ToTable("player_economy_safeguard_states");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.PlayerId).IsUnique();
        b.Property(x => x.PlayerId).IsRequired();
        b.Property(x => x.SessionsStarted).IsRequired();
        b.Property(x => x.LossStreak).IsRequired();
        b.Property(x => x.CurrentEnergy).IsRequired();
        b.Property(x => x.LastEnergyRegenAtUtc).IsRequired();
        b.Property(x => x.LastFreeTicketClaimDate);
        b.Property(x => x.FreeTicketsClaimedToday).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
    }
}
