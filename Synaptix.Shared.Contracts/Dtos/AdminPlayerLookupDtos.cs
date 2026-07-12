namespace Synaptix.Shared.Contracts.Dtos;

public sealed record AdminPlayerLookupResponse(
    string PlayerId,
    string? UserId,
    string ShortCode,
    string? Username,
    string? Email,
    string Source,
    bool Created);

// Multi-result search (GET /admin/player-lookup/search). PlayerId is the raw
// guid so it can feed {id:guid} admin routes directly.
public sealed record AdminPlayerSearchItemDto(
    Guid PlayerId,
    string? Email,
    string? Username,
    int CoinsBalance);

public sealed record AdminPlayerSearchResponse(
    IReadOnlyList<AdminPlayerSearchItemDto> Items,
    int Total);
