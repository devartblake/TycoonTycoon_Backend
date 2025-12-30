using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class ReferralRedemptionConfig : IEntityTypeConfiguration<ReferralRedemption>
    {
        public void Configure(EntityTypeBuilder<ReferralRedemption> b)
        {
            b.ToTable("referral_redemptions");
            b.HasKey(x => x.Id);

            b.Property(x => x.EventId).IsRequired();
            b.HasIndex(x => x.EventId).IsUnique(); // idempotency

            b.Property(x => x.ReferralCodeId).IsRequired();
            b.HasIndex(x => x.ReferralCodeId);

            b.Property(x => x.OwnerPlayerId).IsRequired();
            b.Property(x => x.RedeemerPlayerId).IsRequired();
            b.HasIndex(x => new { x.OwnerPlayerId, x.RedeemerPlayerId });

            b.Property(x => x.RedeemedAtUtc).IsRequired();
        }
    }
}
