namespace Tycoon.Backend.Api.Features.Arcade;

public sealed record ReactorClaimResponse(
    string SpinId,
    string Status,
    bool Duplicate,
    DateTimeOffset AppliedAtUtc,
    IReadOnlyList<ReactorRewardLineDto> Lines,
    ReactorWalletDto Wallet,
    string? ChainedSpinId = null
);

public sealed record ReactorWalletDto(int Coins, int Diamonds, int Xp);
