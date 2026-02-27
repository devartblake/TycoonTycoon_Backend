using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class AdminNotificationChannelConfiguration : IEntityTypeConfiguration<AdminNotificationChannel>
{
    public void Configure(EntityTypeBuilder<AdminNotificationChannel> b)
    {
        b.ToTable("admin_notification_channels");
        b.HasKey(x => x.Key);
        b.Property(x => x.Key).HasMaxLength(100);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Importance).HasMaxLength(32).IsRequired();
        b.Property(x => x.Enabled).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
    }
}
