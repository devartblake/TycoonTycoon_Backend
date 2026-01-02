using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PartyConfiguration : IEntityTypeConfiguration<Party>
    {
        public void Configure(EntityTypeBuilder<Party> b)
        {
            b.ToTable("parties");
            b.HasKey(x => x.Id);

            b.Property(x => x.LeaderPlayerId).IsRequired();

            b.Property(x => x.Status)
                .HasMaxLength(16)
                .IsRequired();

            b.Property(x => x.CreatedAtUtc).IsRequired();

            b.HasIndex(x => new { x.LeaderPlayerId, x.Status });
            b.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        }
    }
}
