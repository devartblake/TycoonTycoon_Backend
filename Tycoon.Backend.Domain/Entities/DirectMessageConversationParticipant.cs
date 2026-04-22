namespace Tycoon.Backend.Domain.Entities
{
    public sealed class DirectMessageConversationParticipant
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid ConversationId { get; private set; }
        public Guid PlayerId { get; private set; }
        public DateTimeOffset JoinedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? LastReadAtUtc { get; private set; }
        public Guid? LastReadMessageId { get; private set; }

        private DirectMessageConversationParticipant() { }

        public DirectMessageConversationParticipant(Guid conversationId, Guid playerId)
        {
            ConversationId = conversationId;
            PlayerId = playerId;
        }

        public void MarkRead(Guid? lastReadMessageId, DateTimeOffset readAtUtc)
        {
            LastReadMessageId = lastReadMessageId;
            LastReadAtUtc = readAtUtc;
        }
    }
}
