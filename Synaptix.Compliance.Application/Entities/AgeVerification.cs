namespace Synaptix.Compliance.Application.Entities;

public sealed class AgeVerification
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public int DeclaredAge { get; init; }
    public bool IsMinor { get; init; }

    // "declaration" | "credit_card" | "id_check"
    public string VerificationMethod { get; init; } = "declaration";
    public DateTimeOffset VerifiedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? IpAddress { get; init; }
}
