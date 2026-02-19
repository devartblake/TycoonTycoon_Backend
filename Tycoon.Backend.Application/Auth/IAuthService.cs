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

        /// <summary>
        /// Creates a new user account with the given credentials.
        /// </summary>
        /// <param name="email">User's email address (must be unique)</param>
        /// <param name="password">Plain text password (will be hashed)</param>
        /// <param name="username">Optional display name (defaults to email prefix)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The created User entity</returns>
        /// <exception cref="InvalidOperationException">If email already exists</exception>
        Task<User> CreateUserAsync(
            string email,
            string password,
            string? username = null,
            CancellationToken ct = default);
    }

    public record AuthResult(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        UserDto User
    );
}
