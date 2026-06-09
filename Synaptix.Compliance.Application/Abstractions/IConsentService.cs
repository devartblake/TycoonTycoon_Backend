using Synaptix.Compliance.Application.Entities;
using Synaptix.Compliance.Contracts.Models;

namespace Synaptix.Compliance.Application.Abstractions;

public interface IConsentService
{
    Task<ConsentRecord> RecordAsync(Guid userId, ConsentType type, bool given, string policyVersion, string? ip, string? userAgent, CancellationToken ct);
    Task<IReadOnlyList<ConsentRecord>> GetCurrentAsync(Guid userId, CancellationToken ct);
    Task<ConsentRecord?> GetLatestAsync(Guid userId, ConsentType type, CancellationToken ct);
}
