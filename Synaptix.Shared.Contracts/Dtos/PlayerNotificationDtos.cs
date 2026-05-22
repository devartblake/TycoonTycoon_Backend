namespace Synaptix.Shared.Contracts.Dtos
{
    public sealed record PlayerNotificationDto(
        Guid Id,
        string Type,
        string Title,
        string Body,
        DateTimeOffset CreatedAtUtc,
        bool Unread,
        string? ActionRoute,
        Dictionary<string, object?> Payload,
        string? Icon,
        string? AvatarUrl);

    public sealed record PlayerNotificationsInboxResponseDto(
        IReadOnlyList<PlayerNotificationDto> Items,
        int Page,
        int PageSize,
        int Total,
        int TotalPages);

    public sealed record UnreadCountResponseDto(int UnreadCount);
}
