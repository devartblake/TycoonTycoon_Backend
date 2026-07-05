using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class AchievementConfig : IEntityTypeConfiguration<Achievement>
    {
        public void Configure(EntityTypeBuilder<Achievement> b)
        {
            b.ToTable("achievements");
            b.HasKey(x => x.Id);

            b.Property(x => x.Key).HasMaxLength(80).IsRequired();
            b.HasIndex(x => x.Key).IsUnique();

            b.Property(x => x.Title).HasMaxLength(120).IsRequired();
            b.Property(x => x.Description).HasMaxLength(600).IsRequired();
            b.Property(x => x.Category).HasMaxLength(80).IsRequired();
            b.Property(x => x.Points).IsRequired();
            b.Property(x => x.IconUrl).HasMaxLength(400);
            b.Property(x => x.IsSecret).IsRequired();
            b.Property(x => x.UpdatedAtUtc).IsRequired();

            b.HasIndex(x => x.Category);
        }
    }

    public sealed class PlayerAchievementConfig : IEntityTypeConfiguration<PlayerAchievement>
    {
        public void Configure(EntityTypeBuilder<PlayerAchievement> b)
        {
            b.ToTable("player_achievements");
            b.HasKey(x => x.Id);

            b.Property(x => x.AchievementKey).HasMaxLength(80).IsRequired();
            b.Property(x => x.UnlockedAtUtc).IsRequired();

            b.HasIndex(x => new { x.PlayerId, x.AchievementKey }).IsUnique();
            b.HasIndex(x => x.PlayerId);
        }
    }
}
