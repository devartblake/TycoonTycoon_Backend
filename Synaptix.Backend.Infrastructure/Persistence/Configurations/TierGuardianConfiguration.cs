using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class TierGuardianConfiguration : IEntityTypeConfiguration<TierGuardian>
    {
        public void Configure(EntityTypeBuilder<TierGuardian> b)
        {
            b.ToTable("tier_guardians");
            b.HasKey(x => x.Id);
            b.Property(x => x.SeasonId).IsRequired();
            b.Property(x => x.TierNumber).IsRequired();
            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.AssignedAtUtc).IsRequired();
            b.Property(x => x.ExpiresAtUtc).IsRequired();
            b.Property(x => x.PassiveCoins).IsRequired();
            b.Property(x => x.PassiveXp).IsRequired();
            b.Property(x => x.DefencesWon).IsRequired();
            b.Property(x => x.DefencesLost).IsRequired();
            b.HasIndex(x => new { x.SeasonId, x.TierNumber, x.PlayerId }).IsUnique();
            b.HasIndex(x => new { x.SeasonId, x.TierNumber });
            b.Ignore(x => x.DomainEvents);
        }
    }
}
