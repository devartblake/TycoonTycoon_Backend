namespace Synaptix.Backend.Domain.Entities
{
    /// <summary>
    /// Immutable admin action audit record with before/after change snapshots (#413).
    /// Complements (does not replace) the AdminNotificationHistory-based security audit:
    /// this table captures WHAT changed on a resource; the security channel captures
    /// auth/session events.
    /// </summary>
    public sealed class AdminAuditLog
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public string Actor { get; private set; } = string.Empty;
        public string Action { get; private set; } = string.Empty;        // dot-namespaced, e.g. "moderation.set_status"
        public string ResourceType { get; private set; } = string.Empty;  // "player", "appeal", "batch", ...
        public string? ResourceId { get; private set; }

        public string? ChangesBeforeJson { get; private set; }
        public string? ChangesAfterJson { get; private set; }

        public string? IpAddress { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private AdminAuditLog() { } // EF

        public AdminAuditLog(
            string actor,
            string action,
            string resourceType,
            string? resourceId,
            string? changesBeforeJson,
            string? changesAfterJson,
            string? ipAddress)
        {
            Actor = actor;
            Action = action;
            ResourceType = resourceType;
            ResourceId = resourceId;
            ChangesBeforeJson = changesBeforeJson;
            ChangesAfterJson = changesAfterJson;
            IpAddress = ipAddress;
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
