namespace Synaptix.Shared.Contracts.Dtos;

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

public record AdminForgotPasswordRequest(
    string Email
);

public record AdminForgotPasswordResponse(
    bool Success,
    string Message
);

public record AdminResetPasswordRequest(
    string Token,
    string NewPassword,
    string ConfirmPassword
);

public record AdminResetPasswordResponse(
    bool Success,
    string Message
);

public record AdminValidateResetTokenRequest(
    string Token
);

public record AdminValidateResetTokenResponse(
    bool Valid,
    string? Email = null,
    string? Message = null
);
