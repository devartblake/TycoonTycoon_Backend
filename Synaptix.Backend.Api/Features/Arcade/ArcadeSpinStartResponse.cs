namespace Synaptix.Backend.Api.Features.Arcade;

public sealed record ArcadeSpinStartResponse(
    string SpinId,
    string Status,
    DateTimeOffset ExpiresAtUtc,
    string RewardId,
    ArcadeSpinAnimationDto Animation,
    ArcadeSpinRewardPreviewDto RewardPreview,
    string ClaimToken
);

public sealed record ArcadeSpinAnimationDto(int WheelStopIndex, string SegmentId, string Rarity);

public sealed record ArcadeSpinRewardPreviewDto(
    string RewardId,
    string DisplayName,
    IReadOnlyList<ReactorRewardLineDto> Lines
);
