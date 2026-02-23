namespace Tycoon.Shared.Contracts.Dtos;

public record AdminLoginRequest(
    string Email,
    string Password,
    string? OtpCode = null
);

public record AdminLoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    AdminProfileResponse Admin
);

public record AdminRefreshResponse(
    string AccessToken,
    int ExpiresIn,
    string TokenType
);

public record AdminProfileResponse(
    string Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions
);
