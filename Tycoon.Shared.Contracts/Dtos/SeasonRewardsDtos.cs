namespace Tycoon.Shared.Contracts.Dtos;

public sealed record RewardEligibilityDto(
    Guid SeasonId,
    Guid PlayerId,
    bool Eligible,
    string Reason,                // "Eligible" | "Placement" | "NotInTop20" | "AlreadyClaimed" | ...
    int Tier,
    int TierRank,

    // Canonical
    int RankPoints,

    int RewardCoins,
    int RewardXp,
    DateTimeOffset? NextClaimAtUtc
)
{
    // Alias    public int SeasonPoints => RankPoints;
};

public sealed record ClaimSeasonRewardRequestDto(
    Guid EventId,                 // idempotency key
    Guid? SeasonId                // optional; server chooses active if null
);

public sealed record ClaimSeasonRewardResponseDto(
    Guid EventId,
    Guid SeasonId,
    Guid PlayerId,
    string Status,                // "Applied" | "Duplicate" | "NotEligible"
    int AwardedCoins,
    int AwardedXp
);
