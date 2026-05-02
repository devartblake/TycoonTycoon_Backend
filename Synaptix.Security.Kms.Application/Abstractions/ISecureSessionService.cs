using Synaptix.Security.Kms.Application.Sessions;
using Synaptix.Security.Kms.Contracts.Models;

namespace Synaptix.Security.Kms.Application.Abstractions;

public interface ISecureSessionService
{
    Task<StartSessionResult> StartAsync(
        string subjectId,
        StartSessionCommand command,
        CancellationToken ct);

    Task<RenewSessionResult> RenewAsync(
        Guid sessionId,
        string subjectId,
        string deviceId,
        CancellationToken ct);

    Task RevokeAsync(Guid sessionId, string reason, CancellationToken ct);

    Task<SecureSession?> GetAsync(Guid sessionId, CancellationToken ct);
}
