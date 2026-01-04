using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PartyMatchMemberConfiguration : IEntityTypeConfiguration<PartyMatchMember>
    {
        public void Configure(EntityTypeBuilder<PartyMatchMember> b)
        {
            b.ToTable("party_match_members");

            b.HasKey(x => new { x.PartyId, x.MatchId, x.PlayerId });

            b.Property(x => x.Role).HasMaxLength(16).IsRequired();
            b.Property(x => x.CapturedAtUtc).IsRequired();

            b.HasIndex(x => x.MatchId);
            b.HasIndex(x => x.PartyId);
        }
    }
}
