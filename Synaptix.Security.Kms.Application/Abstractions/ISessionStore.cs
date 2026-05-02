using Synaptix.Security.Kms.Contracts.Models;

namespace Synaptix.Security.Kms.Application.Abstractions;

public interface ISessionStore
{
    Task SaveAsync(SecureSession session, CancellationToken ct);
    Task<SecureSession?> GetAsync(Guid sessionId, CancellationToken ct);
    Task DeleteAsync(Guid sessionId, CancellationToken ct);
}
