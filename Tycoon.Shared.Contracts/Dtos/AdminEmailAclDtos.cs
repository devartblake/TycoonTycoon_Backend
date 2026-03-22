namespace Tycoon.Shared.Contracts.Dtos;

public record AdminEmailAclEntryDto(
    Guid Id,
    string Email,
    string ListType,
    string Role,
    string? Notes,
    string AddedBy,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public record CreateAdminEmailAclRequest(
    string Email,
    string ListType,
    string Role,
    string? Notes);

public record UpdateAdminEmailAclRequest(
    string ListType,
    string Role,
    string? Notes);

public record AdminEmailAclListResponse(
    IReadOnlyList<AdminEmailAclEntryDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
