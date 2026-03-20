using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class GameEventConfiguration : IEntityTypeConfiguration<GameEvent>
    {
        public void Configure(EntityTypeBuilder<GameEvent> b)
        {
            b.ToTable("game_events");
            b.HasKey(x => x.Id);
            b.Property(x => x.Kind).HasMaxLength(32).IsRequired();
            b.Property(x => x.TierId).IsRequired();
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.ScheduledAtUtc).IsRequired();
            b.Property(x => x.OpenAtUtc);
            b.Property(x => x.EntryFeeCoins).IsRequired();
            b.Property(x => x.ReviveCostGems).IsRequired();
            b.Property(x => x.JackpotPool).IsRequired();
            b.Property(x => x.MaxParticipants).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.ScheduledAtUtc);
            b.HasIndex(x => new { x.Kind, x.Status });
            b.Ignore(x => x.DomainEvents);
        }
    }
}
