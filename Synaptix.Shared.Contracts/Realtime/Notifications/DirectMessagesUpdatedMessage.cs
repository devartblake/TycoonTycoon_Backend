namespace Synaptix.Shared.Contracts.Realtime.Notifications
{
    public sealed record DirectMessagesUpdatedMessage(
        Guid PlayerId,
        Guid? ConversationId,
        int UnreadCount,
        string Reason,
        DateTimeOffset SentAtUtc);
}
