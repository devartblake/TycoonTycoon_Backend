namespace Tycoon.Backend.Domain.Entities
{
    /// <summary>
    /// Represents an accepted friendship edge (directed).
    /// On accept, create two edges: A->B and B->A.
    /// </summary>
    public sealed class FriendEdge
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid PlayerId { get; private set; }
        public Guid FriendPlayerId { get; private set; }

        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private FriendEdge() { } // EF

        public FriendEdge(Guid playerId, Guid friendPlayerId)
        {
            PlayerId = playerId;
            FriendPlayerId = friendPlayerId;
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
