using Synaptix.Security.Kms.Client.Models.Requests;
using Synaptix.Security.Kms.Client.Models.Responses;

namespace Synaptix.Security.Kms.Client.Abstractions;

/// Typed client for the KMS payload-protection surface.
/// Maps to ISecurePayloadProtector from the KMS handoff, exposed over HTTP.
public interface IKmsPayloadClient
{
    /// POST /security/payload/encrypt
    Task<EncryptPayloadResponse> EncryptAsync(
        EncryptPayloadRequest request,
        CancellationToken ct = default);

    /// POST /security/payload/decrypt
    Task<DecryptPayloadResponse> DecryptAsync(
        DecryptPayloadRequest request,
        CancellationToken ct = default);
}
