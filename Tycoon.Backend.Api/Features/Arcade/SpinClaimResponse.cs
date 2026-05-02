namespace Tycoon.Backend.Api.Features.Arcade;

public sealed record SpinClaimResponse(
    bool Success,
    int CoinsGranted,
    int NewBalance,
    string? Message
);
