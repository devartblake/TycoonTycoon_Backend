namespace Tycoon.Security.Kms.Client.Models.Requests;

public sealed record StartSecureSessionRequest(
    string DeviceId,
    string ClientNonce,
    string ClientPublicKey,
    IReadOnlyList<string> SupportedSuites);
