namespace Tycoon.Backend.Domain.Entities
{
    public sealed class DirectMessageConversation
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Type { get; private set; } = "direct";
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        public List<DirectMessageConversationParticipant> Participants { get; private set; } = new();
        public List<DirectMessage> Messages { get; private set; } = new();

        private DirectMessageConversation() { }

        public DirectMessageConversation(Guid firstPlayerId, Guid secondPlayerId)
        {
            Participants.Add(new DirectMessageConversationParticipant(Id, firstPlayerId));
            Participants.Add(new DirectMessageConversationParticipant(Id, secondPlayerId));
        }

        public void Touch(DateTimeOffset? updatedAtUtc = null)
        {
            UpdatedAtUtc = updatedAtUtc ?? DateTimeOffset.UtcNow;
        }
    }
}
