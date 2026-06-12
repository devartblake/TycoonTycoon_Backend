using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Auth
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string email, string password, string deviceId);
        Task<AuthResult> RefreshAsync(string refreshToken);
        Task<AuthResult> AdminLoginAsync(string email, string password, string deviceId);
        Task<AuthResult> AdminRefreshAsync(string refreshToken);
        Task LogoutAsync(string deviceId, Guid userId);
        Task<User> RegisterAsync(string email, string password, string handle, string? country = null);

        /// <summary>
        /// Creates a device-first guest account and issues tokens for it. Used by
        /// POST /auth/device/bootstrap so a player can start immediately and
        /// register later via <see cref="UpgradeAccountAsync"/>.
        /// </summary>
        Task<AuthResult> BootstrapDeviceAsync(string deviceId, string? displayName = null);

        /// <summary>
        /// Promotes the given guest account to a full email account and issues
        /// fresh tokens. Throws <see cref="InvalidOperationException"/> if the
        /// account is already a full account, or the email/handle is taken.
        /// </summary>
        Task<AuthResult> UpgradeAccountAsync(
            Guid userId, string email, string password, string deviceId,
            string? handle = null, string? country = null);
    }

    public record AuthResult(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        UserDto User
    );
}
