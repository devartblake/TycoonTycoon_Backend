using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PartyMemberConfiguration : IEntityTypeConfiguration<PartyMember>
    {
        public void Configure(EntityTypeBuilder<PartyMember> b)
        {
            b.ToTable("party_members");
            b.HasKey(x => x.Id);

            b.Property(x => x.PartyId).IsRequired();
            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.JoinedAtUtc).IsRequired();

            b.HasIndex(x => new { x.PartyId, x.PlayerId }).IsUnique();
            b.HasIndex(x => x.PlayerId);
        }
    }
}
