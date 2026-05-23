namespace Tycoon.Backend.Api.Features.Arcade;

public sealed record SpinClaimRequest(
    string? PlayerId,   // Informational only — authoritative ID comes from JWT
    string? SegmentId,
    string? SpinId,
    string? ClaimToken = null,
    string? IdempotencyKey = null
);
