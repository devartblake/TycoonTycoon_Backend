using Tycoon.Security.Kms.Client.Models.Requests;
using Tycoon.Security.Kms.Client.Models.Responses;

namespace Tycoon.Security.Kms.Client.Abstractions;

/// Typed client for internal service-to-service KMS operations.
/// Maps to IKeyWrappingService and the /internal/security/* endpoints from the KMS handoff.
public interface IKmsInternalClient
{
    /// POST /internal/security/datakey  (IKeyWrappingService.GenerateDataKeyAsync)
    Task<GenerateDataKeyResponse> GenerateDataKeyAsync(
        GenerateDataKeyRequest request,
        CancellationToken ct = default);

    /// POST /internal/security/encrypt
    Task<EncryptPayloadResponse> EncryptAsync(
        EncryptPayloadRequest request,
        CancellationToken ct = default);

    /// POST /internal/security/decrypt
    Task<DecryptPayloadResponse> DecryptAsync(
        DecryptPayloadRequest request,
        CancellationToken ct = default);
}
