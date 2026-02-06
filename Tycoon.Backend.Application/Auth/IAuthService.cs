using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Auth
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string email, string password, string deviceId);
        Task<AuthResult> RefreshAsync(string refreshToken);
        Task LogoutAsync(string deviceId, Guid userId);
        Task<User> RegisterAsync(string email, string password, string handle, string? country = null);
    }

    public record AuthResult(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        UserDto User
    );
}
