namespace Tycoon.Security.Kms.Client.Models.Responses;

public sealed record DecryptPayloadResponse(
    byte[] Plaintext,
    string ContentType);
