namespace Tycoon.Backend.Domain.Entities;

public enum RewardChainTicketStatus
{
    Pending = 0,
    Activated = 1,
    Expired = 2
}

public sealed class RewardChainTicket
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string ChainedSpinId { get; private set; } = string.Empty;
    public Guid PlayerId { get; private set; }
    public string SourceSpinId { get; private set; } = string.Empty;
    public string RewardId { get; private set; } = string.Empty;
    public string RewardLinesJson { get; private set; } = "[]";
    public string AnimationJson { get; private set; } = "{}";
    public RewardChainTicketStatus Status { get; private set; } = RewardChainTicketStatus.Pending;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ActivatedAtUtc { get; private set; }
    public string? GeneratedSpinId { get; private set; }
    public string? GeneratedClaimToken { get; private set; }

    private RewardChainTicket() { } // EF

    public static RewardChainTicket Create(
        Guid playerId,
        string sourceSpinId,
        string rewardId,
        string rewardLinesJson,
        string animationJson,
        DateTimeOffset expiresAtUtc)
    {
        return new RewardChainTicket
        {
            ChainedSpinId = $"chain_{Guid.NewGuid():N}",
            PlayerId = playerId,
            SourceSpinId = sourceSpinId,
            RewardId = rewardId,
            RewardLinesJson = rewardLinesJson,
            AnimationJson = animationJson,
            Status = RewardChainTicketStatus.Pending,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public bool IsExpired() => DateTimeOffset.UtcNow > ExpiresAtUtc;

    public void MarkExpired()
    {
        if (Status == RewardChainTicketStatus.Activated)
            return;

        Status = RewardChainTicketStatus.Expired;
    }

    public void MarkActivated(string generatedSpinId, string generatedClaimToken)
    {
        Status = RewardChainTicketStatus.Activated;
        ActivatedAtUtc = DateTimeOffset.UtcNow;
        GeneratedSpinId = generatedSpinId;
        GeneratedClaimToken = generatedClaimToken;
    }
}
