using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class TerritoryDuelConfiguration : IEntityTypeConfiguration<TerritoryDuel>
    {
        public void Configure(EntityTypeBuilder<TerritoryDuel> b)
        {
            b.ToTable("territory_duels");
            b.HasKey(x => x.Id);
            b.Property(x => x.GameEventId);
            b.Property(x => x.SeasonId).IsRequired();
            b.Property(x => x.TierNumber).IsRequired();
            b.Property(x => x.Category).HasMaxLength(64).IsRequired();
            b.Property(x => x.ChallengerId).IsRequired();
            b.Property(x => x.DefenderId);
            b.Property(x => x.MatchId).IsRequired();
            b.Property(x => x.Outcome);
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.ResolvedAtUtc);
            b.HasIndex(x => x.MatchId).IsUnique();
            b.HasIndex(x => new { x.SeasonId, x.TierNumber, x.Category });
            b.Ignore(x => x.DomainEvents);
        }
    }
}
