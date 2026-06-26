namespace Synaptix.Shared.Contracts.Dtos
{
    // ===== Existing DTOs =====

    public record LoginRequest(string Email, string Password, string DeviceId);

    public record RefreshRequest(string RefreshToken);

    public record LogoutRequest(string DeviceId);

    public record RegisterRequest(string Email, string Password, string Handle, string? Country);

    public record LoginResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        UserDto User
    );

    public record UserDto(
        Guid Id,
        string Handle,
        string Email,
        string? Country,
        string? AvatarUrl,
        string? Tier,
        int Mmr,
        IReadOnlyList<string>? UserRoles = null
    );

    public record UserCareerSummaryDto(
        Guid UserId,
        int Wins,
        int Losses,
        int Draws,
        int MatchesPlayed,
        decimal WinRate
    );

    public record UpdateProfileRequest(string? Handle, string? Country, string? AvatarUrl = null);

    public sealed record AvatarUploadUrlRequest(
        string FileName,
        string ContentType,
        long ContentLength
    );

    public sealed record AvatarUploadUrlResponse(
        string UploadUrl,
        string ObjectKey,
        string PublicUrl
    );

    // ===== User Search DTOs =====

    public sealed record UserSearchResultDto(
        Guid Id,
        string Handle,
        string DisplayName,
        string Username,
        string? AvatarUrl,
        string? Country,
        string? Tier,
        int Mmr
    );

    public sealed record UserSearchResponseDto(
        int Page,
        int PageSize,
        int Total,
        int TotalPages,
        IReadOnlyList<UserSearchResultDto> Items
    );

    // ===== NEW: Signup DTOs =====

    /// <summary>
    /// Request to create a new user account and immediately log in.
    /// This is a combination of Register + Login for mobile apps.
    /// </summary>
    public record SignupRequest(
        string Email,
        string Password,
        string DeviceId,
        string? Username = null,   // Alias for Handle (Flutter sends this)
        string? Handle = null,     // Backend uses this
        string? Country = null
    );

    /// <summary>
    /// Response from signup endpoint - returns auth tokens + user info
    /// (same format as LoginResponse plus UserId as string for Flutter compatibility)
    /// </summary>
    public record SignupResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        string UserId,  // String format for Flutter compatibility
        UserDto User
    );

    // ===== Device-first (guest) auth DTOs =====

    /// <summary>
    /// Request to bootstrap a device-first guest session (POST /auth/device/bootstrap).
    /// The client sends device identity plus optional platform hints.
    /// </summary>
    public record DeviceBootstrapRequest(
        string DeviceId,
        string? DeviceType = null,
        string? Platform = null,
        string? PlatformPlayerId = null,
        string? DisplayName = null
    );

    /// <summary>
    /// Request to upgrade the authenticated guest account into a full email
    /// account (POST /auth/account/upgrade). The user is identified by their
    /// bearer token; this body carries the new credentials.
    /// </summary>
    public record AccountUpgradeRequest(
        string Email,
        string Password,
        string DeviceId,
        string? Username = null,   // Alias for Handle (Flutter sends this)
        string? Handle = null,
        string? Country = null,
        string? DeviceType = null
    );

    // ===== Password Change DTOs =====

    /// <summary>
    /// Request to change the authenticated user's password (POST /auth/change-password).
    /// </summary>
    public record ChangePasswordRequest(
        string CurrentPassword,
        string NewPassword
    );

    /// <summary>
    /// Response from password change endpoint.
    /// </summary>
    public record ChangePasswordResponse(
        string Message,
        bool SessionCleared,
        bool RequiresReauth
    );

    // ===== Password Reset (OTP) DTOs =====

    /// <summary>
    /// Request to initiate password reset (POST /auth/forgot-password).
    /// User requests an OTP be sent via email or SMS.
    /// </summary>
    public record RequestPasswordResetRequest(
        string Email,
        string Method = "email"  // "email" or "sms"
    );

    /// <summary>
    /// Response confirming OTP has been sent.
    /// </summary>
    public record RequestPasswordResetResponse(
        string Message,
        string Method,
        string Hint,
        int ExpiresIn
    );

    /// <summary>
    /// Request to verify OTP (POST /auth/verify-otp).
    /// User provides the OTP they received.
    /// </summary>
    public record VerifyOtpRequest(
        string Email,
        string Otp
    );

    /// <summary>
    /// Response with reset token after OTP verification.
    /// Token is used in the next step to actually reset password.
    /// </summary>
    public record VerifyOtpResponse(
        string Message,
        string ResetToken,
        int ExpiresIn
    );

    /// <summary>
    /// Request to reset password (POST /auth/reset-password).
    /// User provides the reset token and new password.
    /// </summary>
    public record ResetPasswordRequest(
        string Email,
        string Token,
        string NewPassword
    );

    /// <summary>
    /// Response confirming password has been reset.
    /// </summary>
    public record ResetPasswordResponse(
        string Message,
        string Action  // "redirect_to_login"
    );
}
