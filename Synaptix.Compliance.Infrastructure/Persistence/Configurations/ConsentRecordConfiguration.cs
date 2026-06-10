using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Compliance.Application.Entities;

namespace Synaptix.Compliance.Infrastructure.Persistence.Configurations;

internal sealed class ConsentRecordConfiguration : IEntityTypeConfiguration<ConsentRecord>
{
    public void Configure(EntityTypeBuilder<ConsentRecord> b)
    {
        b.ToTable("consent_records");
        b.HasKey(x => x.Id);
        b.Property(x => x.PolicyVersion).HasMaxLength(32).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(45);
        b.Property(x => x.UserAgent).HasMaxLength(512);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => new { x.UserId, x.ConsentType, x.RecordedAt });
    }
}
