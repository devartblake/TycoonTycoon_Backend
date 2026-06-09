using Synaptix.Compliance.Contracts.Models;

namespace Synaptix.Compliance.Application.Entities;

public sealed class ConsentRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public ConsentType ConsentType { get; init; }
    public bool ConsentGiven { get; init; }
    public string PolicyVersion { get; init; } = string.Empty;
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
