namespace Tycoon.Security.Kms.Client.Models.Requests;

public sealed record DecryptPayloadRequest(
    Guid SessionId,
    string Ciphertext,
    string Nonce,
    string Mac,
    string ContentType,
    DateTimeOffset EncryptedAtUtc);
