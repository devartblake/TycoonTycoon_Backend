using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PlayerNotificationConfiguration : IEntityTypeConfiguration<PlayerNotification>
    {
        public void Configure(EntityTypeBuilder<PlayerNotification> b)
        {
            b.ToTable("player_notifications");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.Type).HasMaxLength(32).IsRequired();
            b.Property(x => x.Title).HasMaxLength(160).IsRequired();
            b.Property(x => x.Body).HasMaxLength(1000).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.Unread).IsRequired();
            b.Property(x => x.ActionRoute).HasMaxLength(256);
            b.Property(x => x.PayloadJson).IsRequired();
            b.Property(x => x.Icon).HasMaxLength(64);
            b.Property(x => x.AvatarUrl).HasMaxLength(512);

            b.HasIndex(x => new { x.PlayerId, x.CreatedAtUtc });
            b.HasIndex(x => new { x.PlayerId, x.Unread, x.CreatedAtUtc });
        }
    }
}
