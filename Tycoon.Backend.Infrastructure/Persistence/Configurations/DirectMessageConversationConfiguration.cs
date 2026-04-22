using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class DirectMessageConversationConfiguration : IEntityTypeConfiguration<DirectMessageConversation>
    {
        public void Configure(EntityTypeBuilder<DirectMessageConversation> b)
        {
            b.ToTable("direct_message_conversations");
            b.HasKey(x => x.Id);

            b.Property(x => x.Type).HasMaxLength(16).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc).IsRequired();

            b.HasMany(x => x.Participants)
                .WithOne()
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.Messages)
                .WithOne()
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.Type, x.UpdatedAtUtc });
        }
    }
}
