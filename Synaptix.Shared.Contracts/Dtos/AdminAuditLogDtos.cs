namespace Synaptix.Shared.Contracts.Dtos
{
    public sealed record AdminAuditLogItemDto(
        Guid Id,
        string Actor,
        string Action,
        string ResourceType,
        string? ResourceId,
        Dictionary<string, object>? ChangesBefore,
        Dictionary<string, object>? ChangesAfter,
        string? IpAddress,
        DateTimeOffset CreatedAtUtc
    );

    public sealed record AdminAuditLogListResponseDto(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<AdminAuditLogItemDto> Items
    );
}
