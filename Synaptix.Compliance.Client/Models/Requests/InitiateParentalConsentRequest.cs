namespace Synaptix.Compliance.Client.Models.Requests;

public sealed record InitiateParentalConsentRequest(Guid UserId, string ParentEmail);
