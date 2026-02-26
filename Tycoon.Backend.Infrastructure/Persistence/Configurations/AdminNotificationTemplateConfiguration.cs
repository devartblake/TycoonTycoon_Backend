using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class AdminNotificationTemplateConfiguration : IEntityTypeConfiguration<AdminNotificationTemplate>
{
    public void Configure(EntityTypeBuilder<AdminNotificationTemplate> b)
    {
        b.ToTable("admin_notification_templates");
        b.HasKey(x => x.TemplateId);
        b.Property(x => x.TemplateId).HasMaxLength(100);
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        b.Property(x => x.ChannelKey).HasMaxLength(100).IsRequired();
        b.Property(x => x.VariablesJson).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();

        b.HasIndex(x => x.Name).IsUnique();
    }
}
