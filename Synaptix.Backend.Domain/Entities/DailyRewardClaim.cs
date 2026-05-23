namespace Synaptix.Backend.Domain.Entities;

public sealed class DailyRewardClaim
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PlayerId { get; private set; }
    public DateOnly ClaimDate { get; private set; }
    public int CoinsGranted { get; private set; }
    public DateTimeOffset ClaimedAtUtc { get; private set; }

    private DailyRewardClaim() { } // EF

    public DailyRewardClaim(Guid playerId, DateOnly claimDate, int coinsGranted)
    {
        PlayerId = playerId;
        ClaimDate = claimDate;
        CoinsGranted = coinsGranted;
        ClaimedAtUtc = DateTimeOffset.UtcNow;
    }
}
