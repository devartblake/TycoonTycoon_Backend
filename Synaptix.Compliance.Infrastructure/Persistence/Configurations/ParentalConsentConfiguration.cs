using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Compliance.Application.Entities;

namespace Synaptix.Compliance.Infrastructure.Persistence.Configurations;

internal sealed class ParentalConsentConfiguration : IEntityTypeConfiguration<ParentalConsent>
{
    public void Configure(EntityTypeBuilder<ParentalConsent> b)
    {
        b.ToTable("parental_consents");
        b.HasKey(x => x.Id);
        b.Property(x => x.ParentEmailHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => new { x.UserId, x.Status });
    }
}
