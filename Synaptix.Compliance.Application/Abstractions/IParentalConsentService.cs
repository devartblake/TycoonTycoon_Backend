using Synaptix.Compliance.Contracts.Models;
using ParentalConsentEntity = Synaptix.Compliance.Application.Entities.ParentalConsent;

namespace Synaptix.Compliance.Application.Abstractions;

public interface IParentalConsentService
{
    // Initiates a parental consent request. Returns the raw token for the caller to email the parent.
    Task<(ParentalConsentEntity Record, string RawToken)> InitiateAsync(Guid userId, string parentEmail, string? ip, CancellationToken ct);
    Task<ParentalConsentEntity> VerifyAsync(string rawToken, CancellationToken ct);
    Task<ParentalConsentEntity?> GetStatusAsync(Guid userId, CancellationToken ct);
    Task RevokeAsync(Guid userId, CancellationToken ct);
    Task<ParentalConsentStatus> GetEffectiveStatusAsync(Guid userId, bool isMinor, CancellationToken ct);
}
