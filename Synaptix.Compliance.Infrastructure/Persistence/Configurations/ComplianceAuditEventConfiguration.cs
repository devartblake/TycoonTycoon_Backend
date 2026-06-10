using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Compliance.Application.Entities;

namespace Synaptix.Compliance.Infrastructure.Persistence.Configurations;

internal sealed class ComplianceAuditEventConfiguration : IEntityTypeConfiguration<ComplianceAuditEvent>
{
    public void Configure(EntityTypeBuilder<ComplianceAuditEvent> b)
    {
        b.ToTable("audit_events");
        b.HasKey(x => x.Id);
        b.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        b.Property(x => x.Source).HasMaxLength(64).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(45);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => new { x.EventType, x.OccurredAt });
    }
}
