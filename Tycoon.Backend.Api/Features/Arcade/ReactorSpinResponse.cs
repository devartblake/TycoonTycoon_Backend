namespace Tycoon.Backend.Api.Features.Arcade;

public sealed record ReactorSpinResponse(
    string SpinId,
    string Status,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? CooldownUntilUtc,
    ReactorAnimationDto Animation,
    ReactorRewardPreviewDto RewardPreview,
    string ClaimToken,
    string? EventId = null,
    double? EventMultiplier = null,
    string? SeasonKey = null
);

public sealed record ReactorAnimationDto(
    string Layout,
    string[] Symbols,
    int[] WinningSymbolIndexes,
    string Rarity,
    string Intensity
);

public sealed record ReactorRewardPreviewDto(
    string RewardId,
    string DisplayName,
    IReadOnlyList<ReactorRewardLineDto> Lines
);

public sealed record ReactorRewardLineDto(string Type, int Amount);
