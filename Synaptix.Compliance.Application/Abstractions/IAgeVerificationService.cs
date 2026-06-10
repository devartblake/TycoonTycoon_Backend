using Synaptix.Compliance.Application.Entities;

namespace Synaptix.Compliance.Application.Abstractions;

public interface IAgeVerificationService
{
    Task<AgeVerification> SubmitAsync(Guid userId, int declaredAge, string method, string? ip, CancellationToken ct);
    Task<AgeVerification?> GetLatestAsync(Guid userId, CancellationToken ct);
}
