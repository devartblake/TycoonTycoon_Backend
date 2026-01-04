namespace Tycoon.Backend.Domain.Entities
{
    public sealed class Party
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid LeaderPlayerId { get; private set; }

        // Open | Queued | Matched | Closed
        public string Status { get; private set; } = "Open";

        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private Party() { } // EF

        public Party(Guid leaderPlayerId)
        {
            LeaderPlayerId = leaderPlayerId;
            Status = "Open";
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void MarkQueued()
        {
            if (Status != "Open") return;
            Status = "Queued";
        }

        public void MarkMatched()
        {
            if (Status == "Closed") return;
            Status = "Matched";
        }

        public void Close()
        {
            Status = "Closed";
        }

        public void SetLeader(Guid newLeaderPlayerId)
        {
            if (newLeaderPlayerId == Guid.Empty)
                throw new ArgumentException("newLeaderPlayerId cannot be empty.");
            LeaderPlayerId = newLeaderPlayerId;
        }
    }
}
