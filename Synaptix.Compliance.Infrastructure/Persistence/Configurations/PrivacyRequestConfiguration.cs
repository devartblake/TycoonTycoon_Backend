using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Compliance.Application.Entities;

namespace Synaptix.Compliance.Infrastructure.Persistence.Configurations;

internal sealed class PrivacyRequestConfiguration : IEntityTypeConfiguration<PrivacyRequest>
{
    public void Configure(EntityTypeBuilder<PrivacyRequest> b)
    {
        b.ToTable("privacy_requests");
        b.HasKey(x => x.Id);
        b.Property(x => x.Notes).HasMaxLength(1024);
        b.Property(x => x.IpAddress).HasMaxLength(45);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => new { x.Status, x.SubmittedAt });
    }
}
