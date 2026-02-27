using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class AdminNotificationScheduleConfiguration : IEntityTypeConfiguration<AdminNotificationSchedule>
{
    public void Configure(EntityTypeBuilder<AdminNotificationSchedule> b)
    {
        b.ToTable("admin_notification_schedules");
        b.HasKey(x => x.ScheduleId);
        b.Property(x => x.ScheduleId).HasMaxLength(100);
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        b.Property(x => x.ChannelKey).HasMaxLength(100).IsRequired();
        b.Property(x => x.Status).HasMaxLength(32).IsRequired();
        b.Property(x => x.RetryCount).IsRequired();
        b.Property(x => x.MaxRetries).IsRequired();
        b.Property(x => x.LastError).HasMaxLength(2000);
        b.Property(x => x.ProcessedAtUtc);
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.ScheduledAt).IsRequired();

        b.HasIndex(x => x.ChannelKey);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ScheduledAt);
    }
}
