namespace Synaptix.Compliance.Client.Models.Requests;

public sealed record RecordConsentRequest(
    string ConsentType,
    bool ConsentGiven,
    string PolicyVersion);
