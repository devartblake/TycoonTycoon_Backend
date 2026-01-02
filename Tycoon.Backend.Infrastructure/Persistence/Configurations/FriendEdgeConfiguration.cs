using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class FriendEdgeConfiguration : IEntityTypeConfiguration<FriendEdge>
    {
        public void Configure(EntityTypeBuilder<FriendEdge> b)
        {
            b.ToTable("friend_edges");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.FriendPlayerId).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();

            // Uniqueness of friendship edge (directed)
            b.HasIndex(x => new { x.PlayerId, x.FriendPlayerId }).IsUnique();

            b.HasIndex(x => x.PlayerId);
        }
    }
}
