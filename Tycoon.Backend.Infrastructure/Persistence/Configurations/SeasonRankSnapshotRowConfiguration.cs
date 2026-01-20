using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class SeasonRankSnapshotRowConfiguration : IEntityTypeConfiguration<SeasonRankSnapshotRow>
{
    public void Configure(EntityTypeBuilder<SeasonRankSnapshotRow> b)
    {
        b.ToTable("season_rank_snapshot_rows");
        b.HasKey(x => x.Id);

        b.Property(x => x.SeasonId).IsRequired();
        b.Property(x => x.PlayerId).IsRequired();

        b.Property(x => x.Tier).IsRequired();
        b.Property(x => x.TierRank).IsRequired();
        b.Property(x => x.SeasonRank).IsRequired();
        b.Property(x => x.RankPoints).IsRequired();

        b.Property(x => x.Wins).IsRequired();
        b.Property(x => x.Losses).IsRequired();
        b.Property(x => x.Draws).IsRequired();
        b.Property(x => x.MatchesPlayed).IsRequired();

        b.Property(x => x.CapturedAtUtc).IsRequired();

        // One row per player per season
        b.HasIndex(x => new { x.SeasonId, x.PlayerId }).IsUnique();

        // Fast leaderboard queries by rank
        b.HasIndex(x => new { x.SeasonId, x.SeasonRank });
        b.HasIndex(x => new { x.SeasonId, x.Tier, x.TierRank });
    }
}
