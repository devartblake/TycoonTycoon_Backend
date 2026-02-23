namespace Tycoon.Shared.Contracts.Dtos;

public record AdminUsersListRequest(
    string? Q,
    string? Status,
    string? Role,
    string? AgeGroup,
    bool? IsVerified,
    bool? IsBanned,
    int Page = 1,
    int PageSize = 25,
    string? SortBy = null,
    string? SortOrder = null
);

public record AdminUserListItemDto(
    string Id,
    string Username,
    string Email,
    string Status,
    string Role,
    string AgeGroup,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastActive,
    int TotalGamesPlayed,
    int TotalPoints,
    decimal WinRate,
    bool IsVerified,
    bool IsBanned
);

public record AdminUsersListResponse(
    IReadOnlyList<AdminUserListItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);

public record AdminUserDetailDto(
    string Id,
    string Username,
    string Email,
    string Status,
    string Role,
    string AgeGroup,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastActive,
    int TotalGamesPlayed,
    int TotalPoints,
    decimal WinRate,
    bool IsVerified,
    bool IsBanned,
    Dictionary<string, object> Metadata
);

public record AdminCreateUserRequest(
    string Username,
    string Email,
    string Role,
    string AgeGroup,
    bool IsVerified,
    string TemporaryPassword
);

public record AdminCreateUserResponse(string Id, DateTimeOffset CreatedAt);

public record AdminUpdateUserRequest(
    string? Username = null,
    string? Role = null,
    bool? IsVerified = null
);

public record AdminUpdateUserResponse(string Id, DateTimeOffset UpdatedAt);

public record AdminBanUserRequest(string Reason, DateTimeOffset? Until = null);
public record AdminBanUserResponse(string Id, bool IsBanned, DateTimeOffset? BannedUntil);
public record AdminUnbanUserResponse(string Id, bool IsBanned);

public record AdminUserActivityItemDto(
    string Id,
    string Type,
    string Description,
    DateTimeOffset CreatedAt,
    Dictionary<string, object> Metadata
);

public record AdminUserActivityResponse(
    IReadOnlyList<AdminUserActivityItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);
