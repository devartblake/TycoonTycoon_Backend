using Synaptix.Security.Kms.Contracts.Models;

namespace Synaptix.Security.Kms.Application.Abstractions;

public interface ISecurePayloadProtector
{
    Task<EncryptedPayload> EncryptAsync(
        Guid sessionId,
        byte[] plaintext,
        string contentType,
        CancellationToken ct);

    Task<(byte[] Plaintext, string ContentType)> DecryptAsync(
        Guid sessionId,
        EncryptedPayload payload,
        CancellationToken ct);
}
