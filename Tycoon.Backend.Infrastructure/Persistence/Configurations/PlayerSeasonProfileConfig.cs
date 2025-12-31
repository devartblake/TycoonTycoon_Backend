using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PlayerSeasonProfileConfig : IEntityTypeConfiguration<PlayerSeasonProfile>
    {
        public void Configure(EntityTypeBuilder<PlayerSeasonProfile> b)
        {
            b.ToTable("player_season_profiles");
            b.HasKey(x => x.Id);

            b.Property(x => x.SeasonId).IsRequired();
            b.Property(x => x.PlayerId).IsRequired();
            b.HasIndex(x => new { x.SeasonId, x.PlayerId }).IsUnique();

            b.Property(x => x.RankPoints).IsRequired();
            b.Property(x => x.Wins).IsRequired();
            b.Property(x => x.Losses).IsRequired();
            b.Property(x => x.Draws).IsRequired();
            b.Property(x => x.MatchesPlayed).IsRequired();

            b.Property(x => x.Tier).IsRequired();
            b.Property(x => x.TierRank).IsRequired();
            b.Property(x => x.SeasonRank).IsRequired();

            b.Property(x => x.UpdatedAtUtc).IsRequired();

            b.HasIndex(x => new { x.SeasonId, x.RankPoints });
            b.HasIndex(x => new { x.SeasonId, x.Tier, x.TierRank });
        }
    }
}
