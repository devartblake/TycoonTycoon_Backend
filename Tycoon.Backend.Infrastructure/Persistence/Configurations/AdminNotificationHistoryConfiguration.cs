using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class AdminNotificationHistoryConfiguration : IEntityTypeConfiguration<AdminNotificationHistory>
{
    public void Configure(EntityTypeBuilder<AdminNotificationHistory> b)
    {
        b.ToTable("admin_notification_history");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(100);
        b.Property(x => x.ChannelKey).HasMaxLength(100).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Status).HasMaxLength(64).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.MetadataJson).HasColumnType("jsonb");

        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.ChannelKey);
    }
}
