using Synaptix.Compliance.Client.Models.Requests;
using Synaptix.Compliance.Client.Models.Responses;

namespace Synaptix.Compliance.Client.Abstractions;

public interface IComplianceClient
{
    /// Returns the active feature restrictions for a user (empty list = no restrictions).
    Task<UserRestrictionsResponse> GetUserRestrictionsAsync(Guid userId, CancellationToken ct = default);

    /// Returns the full consent + minor status for a user.
    Task<ConsentStatusResponse> GetConsentStatusAsync(Guid userId, CancellationToken ct = default);

    /// Records a compliance audit event (PCI, CCPA, COPPA actions).
    Task RecordAuditEventAsync(RecordAuditEventRequest request, CancellationToken ct = default);
}
