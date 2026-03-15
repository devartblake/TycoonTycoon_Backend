using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class GameEventParticipant : Entity
    {
        public Guid GameEventId { get; private set; }
        public Guid PlayerId { get; private set; }
        public Guid EntryEventId { get; private set; }
        public DateTimeOffset? EliminatedAt { get; set; }
        public int? FinalRank { get; set; }
        public int RevivesUsed { get; set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }

        private GameEventParticipant() { }

        public GameEventParticipant(Guid gameEventId, Guid playerId, Guid entryEventId)
        {
            GameEventId = gameEventId;
            PlayerId = playerId;
            EntryEventId = entryEventId;
            RevivesUsed = 0;
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
