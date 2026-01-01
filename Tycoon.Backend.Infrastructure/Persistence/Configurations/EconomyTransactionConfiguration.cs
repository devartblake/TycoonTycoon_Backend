using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class EconomyTransactionConfiguration : IEntityTypeConfiguration<EconomyTransaction>
    {
        public void Configure(EntityTypeBuilder<EconomyTransaction> b)
        {
            b.ToTable("economy_transactions");
            b.HasKey(x => x.Id);

            b.Property(x => x.ReversalOfTransactionId);
            b.HasIndex(x => x.ReversalOfTransactionId);

            b.Property(x => x.EventId).IsRequired();
            b.HasIndex(x => x.EventId).IsUnique(); // idempotency

            b.Property(x => x.PlayerId).IsRequired();
            b.HasIndex(x => x.PlayerId);

            b.Property(x => x.Kind).HasMaxLength(64).IsRequired();
            b.Property(x => x.Note).HasMaxLength(512);

            b.Property(x => x.CreatedAtUtc).IsRequired();

            b.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.EconomyTransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
