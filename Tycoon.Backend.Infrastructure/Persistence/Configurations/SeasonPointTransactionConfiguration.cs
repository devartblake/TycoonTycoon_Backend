using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class SeasonPointTransactionConfiguration : IEntityTypeConfiguration<SeasonPointTransaction>
    {
        public void Configure(EntityTypeBuilder<SeasonPointTransaction> b)
        {
            b.ToTable("season_point_transactions");
            b.HasKey(x => x.Id);

            b.Property(x => x.EventId).IsRequired();
            b.HasIndex(x => x.EventId).IsUnique(); // critical for idempotency

            b.Property(x => x.SeasonId).IsRequired();
            b.Property(x => x.PlayerId).IsRequired();

            b.Property(x => x.Kind).HasMaxLength(48).IsRequired();
            b.Property(x => x.Delta).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();

            b.Property(x => x.Note);

            b.HasIndex(x => new { x.SeasonId, x.PlayerId });
            b.HasIndex(x => x.CreatedAtUtc);
        }
    }
}
