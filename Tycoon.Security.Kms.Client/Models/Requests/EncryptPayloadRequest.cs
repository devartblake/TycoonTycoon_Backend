namespace Tycoon.Security.Kms.Client.Models.Requests;

public sealed record EncryptPayloadRequest(
    Guid SessionId,
    byte[] Plaintext,
    string ContentType = "application/json");
