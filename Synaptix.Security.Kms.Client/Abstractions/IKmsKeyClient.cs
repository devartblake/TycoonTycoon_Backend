using Synaptix.Security.Kms.Client.Models.Requests;
using Synaptix.Security.Kms.Client.Models.Responses;

namespace Synaptix.Security.Kms.Client.Abstractions;

/// Typed client for admin/internal key-rotation operations.
/// Maps to POST /security/keys/rotate from the KMS handoff.
public interface IKmsKeyClient
{
    /// POST /security/keys/rotate
    Task<RotateKeyResponse> RotateAsync(
        RotateKeyRequest request,
        CancellationToken ct = default);
}
