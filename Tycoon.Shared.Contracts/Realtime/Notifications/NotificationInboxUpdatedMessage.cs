namespace Tycoon.Shared.Contracts.Realtime.Notifications
{
    public sealed record NotificationInboxUpdatedMessage(
        Guid PlayerId,
        int UnreadCount,
        string Reason,
        DateTimeOffset SentAtUtc);
}
