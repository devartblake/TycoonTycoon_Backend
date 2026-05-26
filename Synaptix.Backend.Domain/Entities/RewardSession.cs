namespace Synaptix.Backend.Domain.Entities;

public sealed class RewardSession
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string SpinId { get; private set; } = string.Empty;
    public Guid PlayerId { get; private set; }
    public RewardMechanism Mechanism { get; private set; }
    public string RewardId { get; private set; } = string.Empty;
    public string RewardLinesJson { get; private set; } = "[]";
    public string AnimationJson { get; private set; } = "{}";
    public RewardSessionStatus Status { get; private set; } = RewardSessionStatus.PendingClaim;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string? ClaimTokenHash { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ClaimedAtUtc { get; private set; }
    public string? PolicySnapshotJson { get; private set; }
    public string? ReactorId { get; private set; }

    private RewardSession() { } // EF

    public static RewardSession Create(
        Guid playerId,
        RewardMechanism mechanism,
        string rewardId,
        string rewardLinesJson,
        string animationJson,
        string idempotencyKey,
        string? claimTokenHash,
        DateTimeOffset expiresAtUtc,
        string? policySnapshotJson = null,
        string? reactorId = null)
    {
        var prefix = mechanism == RewardMechanism.ArcadeSpin ? "spin" : "rr";
        return new RewardSession
        {
            SpinId = $"{prefix}_{Guid.NewGuid():N}",
            PlayerId = playerId,
            Mechanism = mechanism,
            RewardId = rewardId,
            RewardLinesJson = rewardLinesJson,
            AnimationJson = animationJson,
            Status = RewardSessionStatus.PendingClaim,
            IdempotencyKey = idempotencyKey,
            ClaimTokenHash = claimTokenHash,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = expiresAtUtc,
            PolicySnapshotJson = policySnapshotJson,
            ReactorId = reactorId
        };
    }

    public void MarkApplied()
    {
        Status = RewardSessionStatus.Applied;
        ClaimedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkExpired() => Status = RewardSessionStatus.Expired;
    public void MarkRejected() => Status = RewardSessionStatus.Rejected;

    public bool IsExpired() => DateTimeOffset.UtcNow > ExpiresAtUtc;
    public bool IsPendingClaim() => Status == RewardSessionStatus.PendingClaim;
}
