namespace Synaptix.Shared.Contracts.Dtos
{
    public sealed record DirectConversationSummaryDto(
        Guid Id,
        string Type,
        IReadOnlyList<Guid> ParticipantIds,
        string DisplayTitle,
        string? AvatarUrl,
        string? LastMessagePreview,
        DateTimeOffset? LastMessageTimestamp,
        int UnreadCount,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public sealed record DirectConversationListResponseDto(
        IReadOnlyList<DirectConversationSummaryDto> Items,
        int Page,
        int PageSize,
        int Total,
        int TotalPages);

    public sealed record CreateDirectConversationRequestDto(Guid TargetPlayerId);

    public sealed record DirectMessageDto(
        Guid Id,
        Guid ConversationId,
        Guid SenderId,
        string SenderDisplayName,
        string Content,
        string Type,
        string Status,
        DateTimeOffset CreatedAtUtc);

    public sealed record SendDirectMessageRequestDto(
        string Content,
        string? ClientMessageId = null);
}
