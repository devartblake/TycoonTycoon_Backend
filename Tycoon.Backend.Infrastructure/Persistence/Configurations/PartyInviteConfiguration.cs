using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PartyInviteConfiguration : IEntityTypeConfiguration<PartyInvite>
    {
        public void Configure(EntityTypeBuilder<PartyInvite> b)
        {
            b.ToTable("party_invites");
            b.HasKey(x => x.Id);

            b.Property(x => x.PartyId).IsRequired();
            b.Property(x => x.FromPlayerId).IsRequired();
            b.Property(x => x.ToPlayerId).IsRequired();

            b.Property(x => x.Status)
                .HasMaxLength(16)
                .IsRequired();

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.RespondedAtUtc);

            b.HasIndex(x => new { x.ToPlayerId, x.Status, x.CreatedAtUtc });
            b.HasIndex(x => new { x.PartyId, x.Status });
            b.HasIndex(x => new { x.FromPlayerId, x.PartyId, x.Status });
        }
    }
}
