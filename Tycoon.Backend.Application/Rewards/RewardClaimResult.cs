namespace Tycoon.Backend.Application.Rewards;

public sealed record RewardClaimResult(
    string SpinId,
    string Status,
    bool Duplicate,
    DateTimeOffset AppliedAtUtc,
    IReadOnlyList<RewardLine> Lines,
    int WalletCoins,
    int WalletDiamonds,
    int WalletXp,
    string? ChainedSpinId = null,
    string? ErrorCode = null,
    string? ErrorMessage = null
);
