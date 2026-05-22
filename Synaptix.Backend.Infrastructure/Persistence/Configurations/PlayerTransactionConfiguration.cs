using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PlayerTransactionConfiguration : IEntityTypeConfiguration<PlayerTransaction>
    {
        public void Configure(EntityTypeBuilder<PlayerTransaction> b)
        {
            b.ToTable("player_transactions");
            b.HasKey(x => x.Id);

            b.Property(x => x.EventId).IsRequired();
            b.HasIndex(x => x.EventId).IsUnique();

            b.Property(x => x.CorrelatedEventId);
            b.HasIndex(x => x.CorrelatedEventId);

            b.Property(x => x.Kind).HasMaxLength(64).IsRequired();
            b.Property(x => x.Status).IsRequired();

            b.Property(x => x.Receipt).HasMaxLength(2048);
            b.Property(x => x.DisputeReason).HasMaxLength(1024);
            b.Property(x => x.DisputeLinkedToTransactionId);

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.CompletedAtUtc);

            b.HasMany(x => x.Actors)
                .WithOne()
                .HasForeignKey(x => x.PlayerTransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.ItemChanges)
                .WithOne()
                .HasForeignKey(x => x.PlayerTransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.EconomyTransactions)
                .WithOne()
                .HasForeignKey(x => x.PlayerTransactionId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public sealed class PlayerTransactionActorConfiguration : IEntityTypeConfiguration<PlayerTransactionActor>
    {
        public void Configure(EntityTypeBuilder<PlayerTransactionActor> b)
        {
            b.ToTable("player_transaction_actors");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerTransactionId).IsRequired();
            b.HasIndex(x => x.PlayerTransactionId);

            b.Property(x => x.PlayerId).IsRequired();
            b.HasIndex(x => x.PlayerId);

            b.Property(x => x.Role).IsRequired();
            b.Property(x => x.AllocationPercent).IsRequired();
        }
    }

    public sealed class PlayerTransactionItemConfiguration : IEntityTypeConfiguration<PlayerTransactionItem>
    {
        public void Configure(EntityTypeBuilder<PlayerTransactionItem> b)
        {
            b.ToTable("player_transaction_items");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerTransactionId).IsRequired();
            b.HasIndex(x => x.PlayerTransactionId);

            b.Property(x => x.ItemType).HasMaxLength(128).IsRequired();
            b.Property(x => x.Quantity).IsRequired();
            b.Property(x => x.Operation).IsRequired();
        }
    }
}
