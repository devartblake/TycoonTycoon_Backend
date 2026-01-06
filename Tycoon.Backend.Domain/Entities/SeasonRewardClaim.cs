namespace Tycoon.Backend.Domain.Entities;

public sealed class SeasonRewardClaim
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid SeasonId { get; private set; }
    public Guid PlayerId { get; private set; }

    public Guid EventId { get; private set; } // idempotency
    public DateOnly RewardDay { get; private set; } // UTC day boundary

    public int AwardedCoins { get; private set; }
    public int AwardedXp { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private SeasonRewardClaim() { }

    public SeasonRewardClaim(
        Guid seasonId,
        Guid playerId,
        Guid eventId,
        DateOnly rewardDay,
        int awardedCoins,
        int awardedXp)
    {
        SeasonId = seasonId;
        PlayerId = playerId;
        EventId = eventId;
        RewardDay = rewardDay;
        AwardedCoins = awardedCoins;
        AwardedXp = awardedXp;
    }
}
