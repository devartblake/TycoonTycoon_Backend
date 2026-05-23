namespace Synaptix.Backend.Application.Rewards;

public sealed record RewardAnimationHint(
    string Layout,
    string[] Symbols,
    int[] WinningSymbolIndexes,
    string Rarity,
    string Intensity
);
