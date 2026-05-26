namespace Synaptix.Backend.Application.Rewards;

public sealed record RewardPolicyResult(
    bool Allowed,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset? CooldownUntilUtc
);
