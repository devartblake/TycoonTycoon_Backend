using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class MatchmakingTicketConfiguration : IEntityTypeConfiguration<MatchmakingTicket>
    {
        public void Configure(EntityTypeBuilder<MatchmakingTicket> b)
        {
            b.ToTable("matchmaking_tickets");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.Mode).HasMaxLength(32).IsRequired();
            b.Property(x => x.Scope).HasMaxLength(32).IsRequired();
            b.Property(x => x.Status).HasMaxLength(16).IsRequired();

            // Concurrency token
            b.Property(x => x.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Fast "does this player have an active ticket?" checks
            b.HasIndex(x => new { x.PlayerId, x.Status });

            // FIFO opponent query
            b.HasIndex(x => new { x.Mode, x.Tier, x.Scope, x.Status, x.CreatedAtUtc });

            // Optional: cleanup expired tickets
            b.HasIndex(x => new { x.Status, x.ExpiresAtUtc });
        }
    }
}
