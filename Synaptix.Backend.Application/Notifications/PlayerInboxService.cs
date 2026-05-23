using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Personalization;
using Synaptix.Backend.Application.Realtime;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Notifications
{
    public sealed class PlayerInboxService(
        IAppDb db,
        IPlayerNotificationNotifier notifier,
        IPlayerMindProfileService? mindProfiles = null)
    {
        public async Task<PlayerNotificationsInboxResponseDto> GetInboxAsync(Guid playerId, int page, int pageSize, CancellationToken ct)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : Math.Clamp(pageSize, 1, 100);

            var query = db.PlayerNotifications
                .AsNoTracking()
                .Where(x => x.PlayerId == playerId)
                .OrderByDescending(x => x.CreatedAtUtc);

            var total = await query.CountAsync(ct);
            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            var rows = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var items = rows
                .Select(x => new PlayerNotificationDto(
                    x.Id,
                    x.Type,
                    x.Title,
                    x.Body,
                    x.CreatedAtUtc,
                    x.Unread,
                    x.ActionRoute,
                    DeserializePayload(x.PayloadJson),
                    x.Icon,
                    x.AvatarUrl))
                .ToList();

            return new PlayerNotificationsInboxResponseDto(items, page, pageSize, total, totalPages);
        }

        public Task<int> GetUnreadCountAsync(Guid playerId, CancellationToken ct)
            => db.PlayerNotifications.AsNoTracking().CountAsync(x => x.PlayerId == playerId && x.Unread, ct);

        public async Task<string?> MarkReadAsync(Guid playerId, Guid notificationId, CancellationToken ct)
        {
            var notification = await db.PlayerNotifications.FirstOrDefaultAsync(x => x.Id == notificationId, ct);
            if (notification is null)
                return "NotFound";

            if (notification.PlayerId != playerId)
                return "Forbidden";

            notification.MarkRead();
            await db.SaveChangesAsync(ct);
            await NotifyUnreadCountChangedAsync(playerId, "read", ct);
            if (mindProfiles is not null)
            {
                try
                {
                    await mindProfiles.RecordEventAsync(playerId, new PlayerBehaviorEventDto(
                        EventType: "notification_opened",
                        EventSource: "inbox",
                        Category: notification.Type,
                        Difficulty: null,
                        Mode: null,
                        Metadata: null,
                        OccurredAt: DateTimeOffset.UtcNow), ct);
                }
                catch { }
            }
            return null;
        }

        public async Task<int> MarkAllReadAsync(Guid playerId, CancellationToken ct)
        {
            var notifications = await db.PlayerNotifications
                .Where(x => x.PlayerId == playerId && x.Unread)
                .ToListAsync(ct);

            foreach (var notification in notifications)
                notification.MarkRead();

            if (notifications.Count > 0)
                await db.SaveChangesAsync(ct);

            await NotifyUnreadCountChangedAsync(playerId, "read_all", ct);
            return notifications.Count;
        }

        public async Task<string?> DeleteAsync(Guid playerId, Guid notificationId, CancellationToken ct)
        {
            var notification = await db.PlayerNotifications.FirstOrDefaultAsync(x => x.Id == notificationId, ct);
            if (notification is null)
                return "NotFound";

            if (notification.PlayerId != playerId)
                return "Forbidden";

            db.PlayerNotifications.Remove(notification);
            await db.SaveChangesAsync(ct);
            await NotifyUnreadCountChangedAsync(playerId, "deleted", ct);
            if (mindProfiles is not null)
            {
                try
                {
                    await mindProfiles.RecordEventAsync(playerId, new PlayerBehaviorEventDto(
                        EventType: "notification_dismissed",
                        EventSource: "inbox",
                        Category: notification.Type,
                        Difficulty: null,
                        Mode: null,
                        Metadata: null,
                        OccurredAt: DateTimeOffset.UtcNow), ct);
                }
                catch { }
            }
            return null;
        }

        public async Task<PlayerNotificationDto> CreateAsync(
            Guid playerId,
            string type,
            string title,
            string body,
            string? actionRoute,
            Dictionary<string, object?>? payload,
            string? icon,
            string? avatarUrl,
            CancellationToken ct)
        {
            var entity = new Domain.Entities.PlayerNotification(
                playerId,
                type,
                title,
                body,
                actionRoute,
                payload is null ? "{}" : JsonSerializer.Serialize(payload),
                icon,
                avatarUrl);

            db.PlayerNotifications.Add(entity);
            await db.SaveChangesAsync(ct);
            await NotifyUnreadCountChangedAsync(playerId, "created", ct);

            return new PlayerNotificationDto(
                entity.Id,
                entity.Type,
                entity.Title,
                entity.Body,
                entity.CreatedAtUtc,
                entity.Unread,
                entity.ActionRoute,
                DeserializePayload(entity.PayloadJson),
                entity.Icon,
                entity.AvatarUrl);
        }

        private async Task NotifyUnreadCountChangedAsync(Guid playerId, string reason, CancellationToken ct)
        {
            var unreadCount = await GetUnreadCountAsync(playerId, ct);
            await notifier.NotifyInboxUpdatedAsync(playerId, unreadCount, reason, ct);
        }

        private static Dictionary<string, object?> DeserializePayload(string? payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
                return new Dictionary<string, object?>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object?>>(payloadJson)
                    ?? new Dictionary<string, object?>();
            }
            catch
            {
                return new Dictionary<string, object?>();
            }
        }
    }
}
