using Synaptix.Security.Kms.Client.Models.Requests;
using Synaptix.Security.Kms.Client.Models.Responses;

namespace Synaptix.Security.Kms.Client.Abstractions;

/// Typed client for the KMS secure-session surface.
/// Maps to ISecureSessionService from the KMS handoff, exposed over HTTP.
public interface IKmsSessionClient
{
    /// POST /security/sessions/start
    Task<StartSecureSessionResponse> StartAsync(
        StartSecureSessionRequest request,
        CancellationToken ct = default);

    /// POST /security/sessions/renew
    Task<RenewSecureSessionResponse> RenewAsync(
        RenewSecureSessionRequest request,
        CancellationToken ct = default);

    /// POST /security/sessions/revoke
    Task RevokeAsync(
        RevokeSecureSessionRequest request,
        CancellationToken ct = default);
}
