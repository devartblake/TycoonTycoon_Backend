using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class SeasonTiebreakerConfiguration : IEntityTypeConfiguration<SeasonTiebreaker>
    {
        public void Configure(EntityTypeBuilder<SeasonTiebreaker> b)
        {
            b.ToTable("season_tiebreakers");
            b.HasKey(x => x.Id);

            b.Property(x => x.SeasonId).IsRequired();
            b.Property(x => x.Scope).HasMaxLength(32).IsRequired();
            b.Property(x => x.Status).HasMaxLength(16).IsRequired();
            b.Property(x => x.ResolutionNote).HasMaxLength(512);

            // Primitive collection → uuid[] on Npgsql.
            b.Property(x => x.PlayerIds).IsRequired();

            b.Property(x => x.ScheduledAtUtc).IsRequired();
            b.Property(x => x.ExpiresAtUtc).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();

            b.HasIndex(x => x.SeasonId);
            b.HasIndex(x => new { x.Status, x.ExpiresAtUtc }); // expiry sweep
            b.HasIndex(x => x.MatchId);
        }
    }
}
