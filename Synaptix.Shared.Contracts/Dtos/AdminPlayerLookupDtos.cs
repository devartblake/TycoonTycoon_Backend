namespace Synaptix.Shared.Contracts.Dtos;

public sealed record AdminPlayerLookupResponse(
    string PlayerId,
    string? UserId,
    string ShortCode,
    string? Username,
    string? Email,
    string Source,
    bool Created);
