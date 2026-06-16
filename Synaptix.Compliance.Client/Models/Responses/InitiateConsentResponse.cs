namespace Synaptix.Compliance.Client.Models.Responses;

public sealed record InitiateConsentResponse(
    Guid Id,
    string Status,
    DateTimeOffset ExpiresAt,
    string ConsentToken);
