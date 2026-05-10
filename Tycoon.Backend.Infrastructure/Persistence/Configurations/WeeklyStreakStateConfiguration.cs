using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class WeeklyStreakStateConfiguration : IEntityTypeConfiguration<WeeklyStreakState>
{
    public void Configure(EntityTypeBuilder<WeeklyStreakState> builder)
    {
        builder.ToTable("weekly_streak_states");
        builder.HasKey(x => x.Id);
        // One streak record per player
        builder.HasIndex(x => x.PlayerId).IsUnique();
        builder.Property(x => x.PlayerId).IsRequired();
        builder.Property(x => x.CycleStartDate).IsRequired();
        builder.Property(x => x.CurrentDay).IsRequired();
        builder.Property(x => x.ClaimedDaysJson).HasMaxLength(50).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
    }
}
