using AgeVerificationEntity = Synaptix.Compliance.Application.Entities.AgeVerification;

namespace Synaptix.Compliance.Application.Abstractions;

public interface IAgeVerificationService
{
    Task<AgeVerificationEntity> SubmitAsync(Guid userId, int declaredAge, string method, string? ip, CancellationToken ct);
    Task<AgeVerificationEntity?> GetLatestAsync(Guid userId, CancellationToken ct);
}
