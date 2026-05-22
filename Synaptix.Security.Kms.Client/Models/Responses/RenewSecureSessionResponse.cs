namespace Synaptix.Security.Kms.Client.Models.Responses;

public sealed record RenewSecureSessionResponse(
    Guid SessionId,
    DateTimeOffset ExpiresAtUtc);
