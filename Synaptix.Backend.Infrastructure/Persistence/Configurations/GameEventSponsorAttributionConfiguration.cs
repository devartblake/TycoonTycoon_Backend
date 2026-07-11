using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class GameEventSponsorAttributionConfiguration
        : IEntityTypeConfiguration<GameEventSponsorAttribution>
    {
        public void Configure(EntityTypeBuilder<GameEventSponsorAttribution> b)
        {
            b.ToTable("game_event_sponsor_attributions");
            b.HasKey(x => x.Id);

            b.Property(x => x.GameEventId).IsRequired();
            b.Property(x => x.SponsorName).HasMaxLength(80).IsRequired();
            b.Property(x => x.BaseJackpot).IsRequired();
            b.Property(x => x.Multiplier).HasPrecision(5, 2).IsRequired();
            b.Property(x => x.EffectiveJackpot).IsRequired();
            b.Property(x => x.BoostAmount).IsRequired();
            b.Property(x => x.RecordedAtUtc).IsRequired();

            // One attribution per event (idempotent close).
            b.HasIndex(x => x.GameEventId).IsUnique();
            b.HasIndex(x => new { x.SponsorName, x.SeasonId });
        }
    }
}
