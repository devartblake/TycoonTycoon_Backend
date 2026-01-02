using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
    {
        public void Configure(EntityTypeBuilder<FriendRequest> b)
        {
            b.ToTable("friend_requests");
            b.HasKey(x => x.Id);

            b.Property(x => x.FromPlayerId).IsRequired();
            b.Property(x => x.ToPlayerId).IsRequired();

            b.Property(x => x.Status)
                .HasMaxLength(16)
                .IsRequired();

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.RespondedAtUtc);

            // Prevent spam duplicates at query level (service should still enforce rules).
            b.HasIndex(x => new { x.FromPlayerId, x.ToPlayerId, x.Status });
            b.HasIndex(x => new { x.ToPlayerId, x.Status, x.CreatedAtUtc });
        }
    }
}
