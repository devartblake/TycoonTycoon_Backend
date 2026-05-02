namespace Tycoon.Backend.Domain.Entities;

public sealed class SpinClaim
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PlayerId { get; private set; }
    public string SegmentId { get; private set; } = string.Empty;
    public string SpinId { get; private set; } = string.Empty;
    public int CoinsGranted { get; private set; }
    public DateTimeOffset ClaimedAtUtc { get; private set; }

    private SpinClaim() { } // EF

    public SpinClaim(Guid playerId, string segmentId, string spinId, int coinsGranted)
    {
        PlayerId = playerId;
        SegmentId = segmentId;
        SpinId = spinId;
        CoinsGranted = coinsGranted;
        ClaimedAtUtc = DateTimeOffset.UtcNow;
    }
}
