using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PartyMatchmakingTicketConfiguration : IEntityTypeConfiguration<PartyMatchmakingTicket>
    {
        public void Configure(EntityTypeBuilder<PartyMatchmakingTicket> b)
        {
            b.ToTable("party_matchmaking_tickets");
            b.HasKey(x => x.Id);

            b.Property(x => x.PartyId).IsRequired();
            b.Property(x => x.LeaderPlayerId).IsRequired();

            b.Property(x => x.Mode).HasMaxLength(24).IsRequired();
            b.Property(x => x.Scope).HasMaxLength(16).IsRequired();
            b.Property(x => x.Status).HasMaxLength(16).IsRequired();

            b.Property(x => x.Tier).IsRequired();
            b.Property(x => x.PartySize).IsRequired();

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.ExpiresAtUtc).IsRequired();

            // Concurrency token
            b.Property(x => x.RowVersion)
                .IsConcurrencyToken();

            // Indexes for FIFO matching
            b.HasIndex(x => new { x.Status, x.Mode, x.Scope, x.Tier, x.PartySize, x.CreatedAtUtc });
            b.HasIndex(x => new { x.PartyId, x.Status });

            // Protect against multiple queued tickets per party (service is still the ultimate guard)
            b.HasIndex(x => new { x.PartyId, x.Status });
        }
    }
}
