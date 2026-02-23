namespace Tycoon.Shared.Contracts.Dtos;

public record AdminEventQueueUploadRequest(
    string Source,
    DateTimeOffset ExportedAt,
    string PlayerId,
    IReadOnlyList<AdminEventQueueItemRequest> Events
);

public record AdminEventQueueItemRequest(
    string EventId,
    string EventType,
    DateTimeOffset OccurredAt,
    Dictionary<string, object>? Payload,
    int RetryCount
);

public record AdminEventQueueUploadItemResult(string EventId, string Status);

public record AdminEventQueueUploadResponse(
    int Accepted,
    int Rejected,
    int Duplicates,
    IReadOnlyList<AdminEventQueueUploadItemResult> Results
);

public record AdminEventQueueReprocessRequest(string Scope, int Limit = 1000);
public record AdminEventQueueReprocessResponse(string JobId, string Status);
