using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class ProcessedGameplayEventConfig : IEntityTypeConfiguration<ProcessedGameplayEvent>
    {
        public void Configure(EntityTypeBuilder<ProcessedGameplayEvent> b)
        {
            b.ToTable("processed_gameplay_events");

            b.HasKey(x => x.Id);

            b.Property(x => x.EventId).IsRequired();
            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.Kind).HasMaxLength(64).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();

            // Global uniqueness on EventId is enough for idempotency.
            b.HasIndex(x => x.EventId).IsUnique();
        }
    }
}
