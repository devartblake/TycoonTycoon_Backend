namespace Synaptix.Security.Kms.Client.Models.Requests;

public sealed record RenewSecureSessionRequest(
    Guid SessionId,
    string DeviceId);
