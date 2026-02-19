namespace Tycoon.Shared.Contracts.Dtos
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
        string? Tier,
        int Mmr
    );

    public record UpdateProfileRequest(string? Handle, string? Country);

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
}