namespace Tycoon.Backend.Domain.Entities
{
    public enum ModerationStatus
    {
        Normal = 1,
        Suspected = 2,
        Restricted = 3,
        Banned = 4
    }

    /// <summary>
    /// Current moderation state for a player. Use this to enforce runtime behavior.
    /// </summary>
    public sealed class PlayerModerationProfile
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid PlayerId { get; private set; }
        public ModerationStatus Status { get; private set; } = ModerationStatus.Normal;

        public string? Reason { get; private set; }
        public string? Notes { get; private set; }

        public string? SetByAdmin { get; private set; } // admin identifier (email/username) from header or placeholder
        public DateTimeOffset SetAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ExpiresAtUtc { get; private set; } // for temporary restriction/ban

        private PlayerModerationProfile() { } // EF

        public PlayerModerationProfile(Guid playerId)
        {
            PlayerId = playerId;
            Status = ModerationStatus.Normal;
            SetAtUtc = DateTimeOffset.UtcNow;
        }

        public void SetStatus(
            ModerationStatus status,
            string? reason,
            string? notes,
            string? setByAdmin,
            DateTimeOffset? expiresAtUtc)
        {
            Status = status;
            Reason = reason;
            Notes = notes;
            SetByAdmin = setByAdmin;
            ExpiresAtUtc = expiresAtUtc;
            SetAtUtc = DateTimeOffset.UtcNow;
        }

        public bool IsExpired(DateTimeOffset nowUtc)
            => ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= nowUtc;
    }
}
