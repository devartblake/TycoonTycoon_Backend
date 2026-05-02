namespace Synaptix.Security.Kms.Contracts.Models;

public sealed record WrappedDataKey(
    byte[] PlaintextKey,
    string EncryptedKey,
    string KeyVersion);
