using Synaptix.Compliance.Contracts.Models;

namespace Synaptix.Compliance.Application.Entities;

public sealed class ParentalConsent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    // Stored as SHA-256 hashes; plain values are used only transiently for email dispatch / verification.
    public string ParentEmailHash { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;

    public ParentalConsentStatus Status { get; set; } = ParentalConsentStatus.Pending;
    public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? GrantedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
