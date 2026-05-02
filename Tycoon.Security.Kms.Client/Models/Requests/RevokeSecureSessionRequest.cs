namespace Tycoon.Security.Kms.Client.Models.Requests;

public sealed record RevokeSecureSessionRequest(
    Guid SessionId,
    string Reason);
