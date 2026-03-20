using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PlayerEventStatsConfiguration : IEntityTypeConfiguration<PlayerEventStats>
    {
        public void Configure(EntityTypeBuilder<PlayerEventStats> b)
        {
            b.ToTable("player_event_stats");
            b.HasKey(x => x.Id);
            b.Property(x => x.SeasonId).IsRequired();
            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.EventsEntered).IsRequired().HasDefaultValue(0);
            b.Property(x => x.EventsTop20).IsRequired().HasDefaultValue(0);
            b.Property(x => x.EventsWon).IsRequired().HasDefaultValue(0);
            b.Property(x => x.TotalEventXpEarned).IsRequired().HasDefaultValue(0);
            b.Property(x => x.TotalEventCoinsEarned).IsRequired().HasDefaultValue(0);
            b.Property(x => x.ChampionBattleEliminations).IsRequired().HasDefaultValue(0);
            b.Property(x => x.GuardianPromotions).IsRequired().HasDefaultValue(0);
            b.Property(x => x.GuardianDefencesWon).IsRequired().HasDefaultValue(0);
            b.Property(x => x.GuardianDefencesLost).IsRequired().HasDefaultValue(0);
            b.Property(x => x.GuardianDaysTotal).IsRequired().HasDefaultValue(0);
            b.Property(x => x.TilesEverCaptured).IsRequired().HasDefaultValue(0);
            b.Property(x => x.CurrentTilesOwned).IsRequired().HasDefaultValue(0);
            b.Property(x => x.PeakXpMultiplierBps).IsRequired().HasDefaultValue(0);
            b.Property(x => x.UpdatedAtUtc).IsRequired();
            b.HasIndex(x => new { x.SeasonId, x.PlayerId }).IsUnique();
            b.HasIndex(x => new { x.SeasonId, x.EventsWon });
            b.HasIndex(x => new { x.SeasonId, x.GuardianDefencesWon });
            b.HasIndex(x => new { x.SeasonId, x.CurrentTilesOwned });
            b.Ignore(x => x.DomainEvents);
        }
    }
}
