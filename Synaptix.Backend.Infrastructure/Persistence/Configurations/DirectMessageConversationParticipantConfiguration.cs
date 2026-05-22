using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class DirectMessageConversationParticipantConfiguration : IEntityTypeConfiguration<DirectMessageConversationParticipant>
    {
        public void Configure(EntityTypeBuilder<DirectMessageConversationParticipant> b)
        {
            b.ToTable("direct_message_conversation_participants");
            b.HasKey(x => x.Id);

            b.Property(x => x.ConversationId).IsRequired();
            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.JoinedAtUtc).IsRequired();

            b.HasIndex(x => new { x.ConversationId, x.PlayerId }).IsUnique();
            b.HasIndex(x => new { x.PlayerId, x.LastReadAtUtc });
        }
    }
}
