namespace Synaptix.Security.Kms.Client.Models.Responses;

public sealed record StartSecureSessionResponse(
    Guid SessionId,
    string ProtocolVersion,
    string SelectedSuite,
    string ServerPublicKey,
    string ServerNonce,
    DateTimeOffset ExpiresAtUtc,
    string ServerSignature);
