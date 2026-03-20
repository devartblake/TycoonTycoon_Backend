using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class GameEventParticipantConfiguration : IEntityTypeConfiguration<GameEventParticipant>
    {
        public void Configure(EntityTypeBuilder<GameEventParticipant> b)
        {
            b.ToTable("game_event_participants");
            b.HasKey(x => x.Id);
            b.Property(x => x.GameEventId).IsRequired();
            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.EntryEventId).IsRequired();
            b.Property(x => x.EliminatedAt);
            b.Property(x => x.FinalRank);
            b.Property(x => x.RevivesUsed).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.HasIndex(x => new { x.GameEventId, x.PlayerId }).IsUnique();
            b.HasIndex(x => x.EntryEventId).IsUnique();
            b.Ignore(x => x.DomainEvents);
        }
    }
}
