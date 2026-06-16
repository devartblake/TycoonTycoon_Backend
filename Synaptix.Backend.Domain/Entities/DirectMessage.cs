namespace Synaptix.Backend.Domain.Entities
{
    public sealed class DirectMessage
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid ConversationId { get; private set; }
        public Guid SenderId { get; private set; }
        public string Content { get; private set; } = string.Empty;
        public string Type { get; private set; } = "text";
        public string Status { get; private set; } = "delivered";
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public string? ClientMessageId { get; private set; }

        private DirectMessage() { }

        public DirectMessage(Guid conversationId, Guid senderId, string content, string? clientMessageId = null)
        {
            ConversationId = conversationId;
            SenderId = senderId;
            Content = content.Trim();
            ClientMessageId = string.IsNullOrWhiteSpace(clientMessageId) ? null : clientMessageId.Trim();
        }

        public void Redact()
        {
            Content = "[message deleted]";
        }
    }
}
