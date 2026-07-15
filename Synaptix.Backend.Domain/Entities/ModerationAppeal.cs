namespace Synaptix.Backend.Domain.Entities
{
    public enum ModerationAppealStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }

    /// <summary>
    /// A player's appeal against a moderation sanction. Submitted by the player,
    /// reviewed by an admin; approval lifts the sanction via the moderation pipeline.
    /// </summary>
    public sealed class ModerationAppeal
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid PlayerId { get; private set; }

        public string Reason { get; private set; } = string.Empty;

        public ModerationAppealStatus Status { get; private set; } = ModerationAppealStatus.Pending;

        public string? ReviewerNotes { get; private set; }
        public string? ReviewedBy { get; private set; }

        public DateTimeOffset SubmittedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ReviewedAtUtc { get; private set; }

        private ModerationAppeal() { } // EF

        public ModerationAppeal(Guid playerId, string reason)
        {
            PlayerId = playerId;
            Reason = reason;
            SubmittedAtUtc = DateTimeOffset.UtcNow;
        }

        public void Review(ModerationAppealStatus verdict, string? reviewerNotes, string? reviewedBy)
        {
            if (Status != ModerationAppealStatus.Pending)
                throw new InvalidOperationException($"Appeal {Id} has already been reviewed ({Status}).");
            if (verdict == ModerationAppealStatus.Pending)
                throw new ArgumentException("Review verdict must be Approved or Rejected.", nameof(verdict));

            Status = verdict;
            ReviewerNotes = reviewerNotes;
            ReviewedBy = reviewedBy;
            ReviewedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
