namespace Tycoon.Shared.Contracts.Dtos
{
    public sealed record ModerationProfileDto(
        Guid PlayerId,
        int Status,
        string? Reason,
        string? Notes,
        string? SetByAdmin,
        DateTimeOffset SetAtUtc,
        DateTimeOffset? ExpiresAtUtc
    );

    public sealed record SetModerationStatusRequest(
        Guid PlayerId,
        int Status,                 // matches ModerationStatus enum int values
        string? Reason,
        string? Notes,
        DateTimeOffset? ExpiresAtUtc,
        Guid? RelatedFlagId
    );

    public sealed record ModerationLogItemDto(
        Guid Id,
        Guid PlayerId,
        int NewStatus,
        string? Reason,
        string? Notes,
        string? SetByAdmin,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? ExpiresAtUtc,
        Guid? RelatedFlagId
    );

    public sealed record ModerationLogListResponseDto(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<ModerationLogItemDto> Items
    );
}
