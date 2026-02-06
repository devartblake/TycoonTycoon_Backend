namespace Tycoon.Shared.Contracts.Dtos
{
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
}
