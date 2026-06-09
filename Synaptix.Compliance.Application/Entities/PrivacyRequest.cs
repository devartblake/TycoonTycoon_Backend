using Synaptix.Compliance.Contracts.Models;

namespace Synaptix.Compliance.Application.Entities;

public sealed class PrivacyRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public PrivacyRequestType RequestType { get; init; }
    public PrivacyRequestStatus Status { get; set; } = PrivacyRequestStatus.Pending;
    public DateTimeOffset SubmittedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public string? IpAddress { get; init; }
}
