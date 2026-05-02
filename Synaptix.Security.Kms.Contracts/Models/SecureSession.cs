namespace Synaptix.Security.Kms.Contracts.Models;

public sealed record SecureSession(
    Guid SessionId,
    string SubjectId,
    string DeviceId,
    string ProtocolVersion,
    string Suite,
    byte[] ClientToServerKey,
    byte[] ServerToClientKey,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    long LastSequence);
