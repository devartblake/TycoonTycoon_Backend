using Synaptix.Compliance.Client.Models.Requests;
using Synaptix.Compliance.Client.Models.Responses;

namespace Synaptix.Compliance.Client.Abstractions;

public interface IComplianceClient
{
    // Returns the active feature restrictions for a user (empty list = no restrictions).
    Task<UserRestrictionsResponse> GetUserRestrictionsAsync(Guid userId, CancellationToken ct = default);

    // Returns the full consent + minor status for a user.
    Task<ConsentStatusResponse> GetConsentStatusAsync(Guid userId, CancellationToken ct = default);

    // Records a compliance audit event (PCI, CCPA, COPPA actions).
    Task RecordAuditEventAsync(RecordAuditEventRequest request, CancellationToken ct = default);

    // Initiates a parental consent request (server-side). Returns the raw token for email dispatch.
    Task<InitiateConsentResponse> InitiateParentalConsentAsync(InitiateParentalConsentRequest request, CancellationToken ct = default);

    // Returns pending privacy requests for fulfillment processing.
    Task<IReadOnlyList<PendingPrivacyRequestItem>> GetPendingPrivacyRequestsAsync(int limit = 50, CancellationToken ct = default);

    // Marks a privacy request as completed or failed.
    Task CompletePrivacyRequestAsync(Guid requestId, string status, string? notes, CancellationToken ct = default);
}
