using Synaptix.Security.Kms.Contracts.Models;

namespace Synaptix.Security.Kms.Application.Abstractions;

public interface ISecurePayloadProtector
{
    Task<EncryptedPayload> EncryptAsync(
        Guid sessionId,
        byte[] plaintext,
        string contentType,
        CancellationToken ct,
        string? aad = null,
        string direction = "server-to-client");

    Task<(byte[] Plaintext, string ContentType)> DecryptAsync(
        Guid sessionId,
        EncryptedPayload payload,
        CancellationToken ct,
        long? sequenceNumber = null,
        string? replayNonce = null,
        string? aad = null,
        string? subjectId = null,
        string direction = "client-to-server",
        bool enforceReplay = true);
}
