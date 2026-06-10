using Synaptix.Compliance.Application.Entities;
using Synaptix.Compliance.Contracts.Models;

namespace Synaptix.Compliance.Application.Abstractions;

public interface IPrivacyRequestService
{
    Task<PrivacyRequest> SubmitAsync(Guid userId, PrivacyRequestType type, string? ip, CancellationToken ct);
    Task<PrivacyRequest?> GetAsync(Guid requestId, CancellationToken ct);
    Task<IReadOnlyList<PrivacyRequest>> GetPendingAsync(int limit, CancellationToken ct);
    Task<PrivacyRequest> UpdateStatusAsync(Guid requestId, PrivacyRequestStatus status, string? notes, CancellationToken ct);
}
