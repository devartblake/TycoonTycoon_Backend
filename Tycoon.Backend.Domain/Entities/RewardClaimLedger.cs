namespace Tycoon.Backend.Domain.Entities;

public sealed class RewardClaimLedger
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PlayerId { get; private set; }
    public RewardMechanism Mechanism { get; private set; }
    public string SpinId { get; private set; } = string.Empty;
    public string RewardId { get; private set; } = string.Empty;
    public string RewardLinesJson { get; private set; } = "[]";
    public string ClaimStatus { get; private set; } = "Applied";
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTimeOffset AppliedAtUtc { get; private set; }
    public Guid AuditCorrelationId { get; private set; }

    private RewardClaimLedger() { } // EF

    public static RewardClaimLedger Create(
        Guid playerId,
        RewardMechanism mechanism,
        string spinId,
        string rewardId,
        string rewardLinesJson,
        string claimStatus,
        string idempotencyKey)
    {
        return new RewardClaimLedger
        {
            PlayerId = playerId,
            Mechanism = mechanism,
            SpinId = spinId,
            RewardId = rewardId,
            RewardLinesJson = rewardLinesJson,
            ClaimStatus = claimStatus,
            IdempotencyKey = idempotencyKey,
            AppliedAtUtc = DateTimeOffset.UtcNow,
            AuditCorrelationId = Guid.NewGuid()
        };
    }
}
