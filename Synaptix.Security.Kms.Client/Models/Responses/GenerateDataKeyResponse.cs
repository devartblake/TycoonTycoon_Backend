namespace Synaptix.Security.Kms.Client.Models.Responses;

public sealed record GenerateDataKeyResponse(
    byte[] PlaintextKey,
    string EncryptedKey,
    string KeyVersion);
