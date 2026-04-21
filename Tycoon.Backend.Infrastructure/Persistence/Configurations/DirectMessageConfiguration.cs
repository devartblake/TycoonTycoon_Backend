using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class DirectMessageConfiguration : IEntityTypeConfiguration<DirectMessage>
    {
        public void Configure(EntityTypeBuilder<DirectMessage> b)
        {
            b.ToTable("direct_messages");
            b.HasKey(x => x.Id);

            b.Property(x => x.ConversationId).IsRequired();
            b.Property(x => x.SenderId).IsRequired();
            b.Property(x => x.Content).HasMaxLength(4000).IsRequired();
            b.Property(x => x.Type).HasMaxLength(16).IsRequired();
            b.Property(x => x.Status).HasMaxLength(16).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.ClientMessageId).HasMaxLength(128);

            b.HasIndex(x => new { x.ConversationId, x.CreatedAtUtc });
            b.HasIndex(x => new { x.ConversationId, x.SenderId, x.ClientMessageId }).IsUnique();
        }
    }
}
