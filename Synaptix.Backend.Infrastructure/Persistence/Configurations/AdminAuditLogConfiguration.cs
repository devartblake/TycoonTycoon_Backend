using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
    {
        public void Configure(EntityTypeBuilder<AdminAuditLog> b)
        {
            b.ToTable("admin_audit_logs");
            b.HasKey(x => x.Id);

            b.Property(x => x.Actor).IsRequired().HasMaxLength(200);
            b.HasIndex(x => x.Actor);

            b.Property(x => x.Action).IsRequired().HasMaxLength(100);
            b.HasIndex(x => x.Action);

            b.Property(x => x.ResourceType).IsRequired().HasMaxLength(100);
            b.Property(x => x.ResourceId).HasMaxLength(100);
            b.HasIndex(x => new { x.ResourceType, x.ResourceId });

            b.Property(x => x.ChangesBeforeJson);
            b.Property(x => x.ChangesAfterJson);

            b.Property(x => x.IpAddress).HasMaxLength(64);

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.HasIndex(x => x.CreatedAtUtc);
        }
    }
}
