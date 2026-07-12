namespace Synaptix.Backend.Domain.Entities
{
    public sealed class FriendRequest
    {
        /// <summary>Valid status values for a <see cref="FriendRequest"/>.</summary>
        public static class Statuses
        {
            public const string Pending   = "Pending";
            public const string Accepted  = "Accepted";
            public const string Declined  = "Declined";
            public const string Cancelled = "Cancelled";
        }

        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid FromPlayerId { get; private set; }
        public Guid ToPlayerId { get; private set; }

        // Pending | Accepted | Declined | Cancelled
        public string Status { get; private set; } = Statuses.Pending;

        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? RespondedAtUtc { get; private set; }

        private FriendRequest() { } // EF

        public FriendRequest(Guid fromPlayerId, Guid toPlayerId)
        {
            FromPlayerId = fromPlayerId;
            ToPlayerId = toPlayerId;
            Status = Statuses.Pending;
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void Accept()
        {
            if (Status != Statuses.Pending) return;
            Status = Statuses.Accepted;
            RespondedAtUtc = DateTimeOffset.UtcNow;
        }

        public void Decline()
        {
            if (Status != Statuses.Pending) return;
            Status = Statuses.Declined;
            RespondedAtUtc = DateTimeOffset.UtcNow;
        }

        public void Cancel()
        {
            if (Status != Statuses.Pending) return;
            Status = Statuses.Cancelled;
            RespondedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
