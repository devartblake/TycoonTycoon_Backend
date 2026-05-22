namespace Synaptix.Security.Kms.Client.Models.Responses;

public sealed record EncryptPayloadResponse(
    string Ciphertext,
    string Nonce,
    string Mac,
    string ContentType,
    DateTimeOffset EncryptedAtUtc);
