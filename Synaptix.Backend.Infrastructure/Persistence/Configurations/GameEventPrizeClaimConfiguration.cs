using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class GameEventPrizeClaimConfiguration : IEntityTypeConfiguration<GameEventPrizeClaim>
    {
        public void Configure(EntityTypeBuilder<GameEventPrizeClaim> b)
        {
            b.ToTable("game_event_prize_claims");
            b.HasKey(x => x.Id);
            b.Property(x => x.GameEventId).IsRequired();
            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.EventId).IsRequired();
            b.Property(x => x.AwardedXp).IsRequired();
            b.Property(x => x.AwardedCoins).IsRequired();
            b.Property(x => x.Rank).IsRequired();
            b.Property(x => x.ClaimedAtUtc).IsRequired();
            b.HasIndex(x => x.EventId).IsUnique();
            b.HasIndex(x => new { x.GameEventId, x.PlayerId }).IsUnique();
            b.Ignore(x => x.DomainEvents);
        }
    }
}
