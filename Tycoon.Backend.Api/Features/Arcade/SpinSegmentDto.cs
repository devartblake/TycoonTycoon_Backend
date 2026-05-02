namespace Tycoon.Backend.Api.Features.Arcade;

public sealed record SpinSegmentDto(
    string Id,
    string Label,
    string Color,
    string? ImagePath,
    string RewardType,
    int Reward,
    bool IsExclusive,
    int RequiredStreak,
    int RequiredCurrency,
    string? Description,
    double Probability,
    Dictionary<string, object>? Metadata,
    bool IsEnabled,
    DateTimeOffset? EnabledUntil
);
