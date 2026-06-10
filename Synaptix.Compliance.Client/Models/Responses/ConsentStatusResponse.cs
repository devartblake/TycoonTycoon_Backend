namespace Synaptix.Compliance.Client.Models.Responses;

public sealed record ConsentStatusResponse(
    Guid UserId,
    bool IsMinor,
    string ParentalConsent,
    IReadOnlyList<ConsentEntry> Consents);

public sealed record ConsentEntry(
    string ConsentType,
    bool ConsentGiven,
    string PolicyVersion,
    DateTimeOffset RecordedAt);
