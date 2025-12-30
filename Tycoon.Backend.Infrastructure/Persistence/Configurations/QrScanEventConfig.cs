using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class QrScanEventConfig : IEntityTypeConfiguration<QrScanEvent>
    {
        public void Configure(EntityTypeBuilder<QrScanEvent> b)
        {
            b.ToTable("qr_scan_events");
            b.HasKey(x => x.Id);

            b.Property(x => x.EventId).IsRequired();
            b.HasIndex(x => x.EventId).IsUnique(); // idempotency

            b.Property(x => x.PlayerId).IsRequired();
            b.HasIndex(x => x.PlayerId);

            b.Property(x => x.Value).HasMaxLength(512).IsRequired();
            b.Property(x => x.OccurredAtUtc).IsRequired();
            b.Property(x => x.StoredAtUtc).IsRequired();

            b.Property(x => x.Type).IsRequired();
            b.HasIndex(x => new { x.PlayerId, x.Type, x.OccurredAtUtc });
        }
    }
}
