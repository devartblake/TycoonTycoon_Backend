namespace Synaptix.Security.Kms.Application.Sessions;

public sealed record StartSessionResult(
    Guid SessionId,
    string ProtocolVersion,
    string SelectedSuite,
    string ServerPublicKey,
    string ServerNonce,
    DateTimeOffset ExpiresAtUtc,
    string ServerSignature);
