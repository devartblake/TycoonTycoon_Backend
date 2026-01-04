using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PartyMatchLinkConfiguration : IEntityTypeConfiguration<PartyMatchLink>
    {
        public void Configure(EntityTypeBuilder<PartyMatchLink> b)
        {
            b.ToTable("party_match_links");
            b.HasKey(x => x.Id);

            b.Property(x => x.PartyId).IsRequired();
            b.Property(x => x.MatchId).IsRequired();
            b.Property(x => x.Status).HasMaxLength(16).IsRequired();

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.ClosedAtUtc);

            // One link per party per match
            b.HasIndex(x => new { x.PartyId, x.MatchId }).IsUnique();

            // Fast close lookup by match
            b.HasIndex(x => new { x.MatchId, x.Status });
        }
    }
}
