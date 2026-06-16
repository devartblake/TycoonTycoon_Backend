namespace Synaptix.Compliance.Client.Models.Responses;

public sealed record PendingPrivacyRequestItem(
    Guid Id,
    Guid UserId,
    string RequestType,
    string Status,
    DateTimeOffset SubmittedAt);
