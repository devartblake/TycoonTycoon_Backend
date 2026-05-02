namespace Synaptix.Security.Kms.Application.Sessions;

public sealed record StartSessionCommand(
    string DeviceId,
    string ClientNonce,
    string ClientPublicKey,
    IReadOnlyList<string> SupportedSuites);
