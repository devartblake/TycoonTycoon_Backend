using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Compliance.Application.Entities;

namespace Synaptix.Compliance.Infrastructure.Persistence.Configurations;

internal sealed class AgeVerificationConfiguration : IEntityTypeConfiguration<AgeVerification>
{
    public void Configure(EntityTypeBuilder<AgeVerification> b)
    {
        b.ToTable("age_verifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.VerificationMethod).HasMaxLength(32).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(45);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => new { x.UserId, x.VerifiedAt });
    }
}
