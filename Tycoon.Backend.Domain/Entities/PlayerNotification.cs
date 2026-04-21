using System.Text.Json;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class PlayerNotification
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PlayerId { get; private set; }
        public string Type { get; private set; } = "notification";
        public string Title { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public bool Unread { get; private set; } = true;
        public string? ActionRoute { get; private set; }
        public string PayloadJson { get; private set; } = "{}";
        public string? Icon { get; private set; }
        public string? AvatarUrl { get; private set; }
        public DateTimeOffset? ReadAtUtc { get; private set; }

        private PlayerNotification() { }

        public PlayerNotification(
            Guid playerId,
            string type,
            string title,
            string body,
            string? actionRoute,
            string? payloadJson,
            string? icon,
            string? avatarUrl)
        {
            PlayerId = playerId;
            Type = string.IsNullOrWhiteSpace(type) ? "notification" : type.Trim();
            Title = title.Trim();
            Body = body.Trim();
            ActionRoute = string.IsNullOrWhiteSpace(actionRoute) ? null : actionRoute.Trim();
            PayloadJson = NormalizePayloadJson(payloadJson);
            Icon = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim();
            AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        }

        public void MarkRead()
        {
            if (!Unread)
                return;

            Unread = false;
            ReadAtUtc = DateTimeOffset.UtcNow;
        }

        public void MarkUnread()
        {
            Unread = true;
            ReadAtUtc = null;
        }

        private static string NormalizePayloadJson(string? payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
                return "{}";

            try
            {
                using var doc = JsonDocument.Parse(payloadJson);
                return doc.RootElement.GetRawText();
            }
            catch
            {
                return "{}";
            }
        }
    }
}
