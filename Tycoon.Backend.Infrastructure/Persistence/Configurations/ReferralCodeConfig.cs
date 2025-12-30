using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class ReferralCodeConfig : IEntityTypeConfiguration<ReferralCode>
    {
        public void Configure(EntityTypeBuilder<ReferralCode> b)
        {
            b.ToTable("referral_codes");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(32).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();

            b.Property(x => x.OwnerPlayerId).IsRequired();
            b.HasIndex(x => x.OwnerPlayerId);
            b.Property(x => x.CreatedAtUtc).IsRequired();
        }
    }
}
