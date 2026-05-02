namespace Tycoon.Security.Kms.Client.Models.Responses;

public sealed record RotateKeyResponse(
    string KeyName,
    string NewKeyVersion,
    DateTimeOffset RotatedAtUtc);
