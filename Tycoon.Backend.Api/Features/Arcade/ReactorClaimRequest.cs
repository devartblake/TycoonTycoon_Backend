namespace Tycoon.Backend.Api.Features.Arcade;

public sealed record ReactorClaimRequest(
    string SpinId,
    string IdempotencyKey,
    string ClaimToken
);
