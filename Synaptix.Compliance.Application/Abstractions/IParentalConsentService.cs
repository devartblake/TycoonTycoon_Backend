using Synaptix.Compliance.Application.Entities;
using Synaptix.Compliance.Contracts.Models;

namespace Synaptix.Compliance.Application.Abstractions;

public interface IParentalConsentService
{
    // Initiates a parental consent request. Returns the raw token for the caller to email the parent.
    Task<(ParentalConsent Record, string RawToken)> InitiateAsync(Guid userId, string parentEmail, string? ip, CancellationToken ct);
    Task<ParentalConsent> VerifyAsync(string rawToken, CancellationToken ct);
    Task<ParentalConsent?> GetStatusAsync(Guid userId, CancellationToken ct);
    Task RevokeAsync(Guid userId, CancellationToken ct);
    Task<ParentalConsentStatus> GetEffectiveStatusAsync(Guid userId, bool isMinor, CancellationToken ct);
}
