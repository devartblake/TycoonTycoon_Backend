namespace Synaptix.Security.Kms.Contracts.Models;

public sealed record EncryptedPayload(
    string Ciphertext,
    string Nonce,
    string Mac,
    string ContentType,
    DateTimeOffset EncryptedAtUtc);
