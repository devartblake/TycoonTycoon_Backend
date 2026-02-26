namespace Tycoon.Shared.Contracts.Dtos;

public record AdminNotificationChannelDto(
    string Key,
    string Name,
    string Description,
    string Importance,
    bool Enabled
);

public record UpsertAdminNotificationChannelRequest(
    string Name,
    string Description,
    string Importance,
    bool Enabled
);

public record AdminNotificationSendRequest(
    string Title,
    string Body,
    string ChannelKey,
    Dictionary<string, object> Audience,
    Dictionary<string, object>? Payload
);

public record AdminNotificationSendResponse(string JobId, int EstimatedRecipients);

public record AdminNotificationScheduleRequest(
    string Title,
    string Body,
    string ChannelKey,
    DateTimeOffset ScheduledAt,
    Dictionary<string, object>? Repeat,
    Dictionary<string, object> Audience
);

public record AdminNotificationScheduleResponse(string ScheduleId);

public record AdminNotificationScheduledItemDto(
    string ScheduleId,
    string Title,
    string ChannelKey,
    DateTimeOffset ScheduledAt,
    string Status
);

public record AdminNotificationScheduledListResponse(
    IReadOnlyList<AdminNotificationScheduledItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);

public record AdminNotificationTemplateRequest(
    string Name,
    string Title,
    string Body,
    string ChannelKey,
    IReadOnlyList<string> Variables
);

public record AdminNotificationTemplateDto(
    string TemplateId,
    string Name,
    string Title,
    string Body,
    string ChannelKey,
    IReadOnlyList<string> Variables,
    DateTimeOffset UpdatedAt
);

public record AdminNotificationHistoryItemDto(
    string Id,
    string ChannelKey,
    string Title,
    string Status,
    DateTimeOffset CreatedAt,
    Dictionary<string, object>? Metadata
);

public record AdminNotificationHistoryResponse(
    IReadOnlyList<AdminNotificationHistoryItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);
