namespace Tycoon.Shared.Contracts.Dtos
{
    public record LoginRequest(
        string Email,
        string Password,
        string DeviceId
    );

    public record LoginResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        UserDto User
    );

    public record RefreshRequest(
        string RefreshToken
    );

    public record LogoutRequest(
        string DeviceId
    );

    public record RegisterRequest(
        string Email,
        string Password,
        string Handle,
        string Country
    );

    public record UserDto(
        Guid Id,
        string Email,
        string Handle,
        string Country,
        string? AvatarUrl,
        DateTimeOffset CreatedAt
    );

    public record UpdateProfileRequest(
        string Handle,
        string Country
    );
}
