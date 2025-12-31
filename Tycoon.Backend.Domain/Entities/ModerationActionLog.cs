namespace Tycoon.Backend.Domain.Entities
{
    /// <summary>
    /// Immutable audit record of moderation changes.
    /// </summary>
    public sealed class ModerationActionLog
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid PlayerId { get; private set; }

        public ModerationStatus NewStatus { get; private set; }
        public string? Reason { get; private set; }
        public string? Notes { get; private set; }

        public string? SetByAdmin { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ExpiresAtUtc { get; private set; }

        public Guid? RelatedFlagId { get; private set; } // optional link to AntiCheatFlag.Id

        private ModerationActionLog() { } // EF

        public ModerationActionLog(
            Guid playerId,
            ModerationStatus newStatus,
            string? reason,
            string? notes,
            string? setByAdmin,
            DateTimeOffset? expiresAtUtc,
            Guid? relatedFlagId)
        {
            PlayerId = playerId;
            NewStatus = newStatus;
            Reason = reason;
            Notes = notes;
            SetByAdmin = setByAdmin;
            ExpiresAtUtc = expiresAtUtc;
            RelatedFlagId = relatedFlagId;
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
