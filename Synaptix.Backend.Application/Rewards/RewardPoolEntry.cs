namespace Synaptix.Backend.Application.Rewards;

public sealed record RewardPoolEntry(
    string RewardId,
    string DisplayName,
    IReadOnlyList<RewardLine> Lines,
    RewardAnimationHint Animation,
    double Weight,
    bool IsEnabled = true
);
