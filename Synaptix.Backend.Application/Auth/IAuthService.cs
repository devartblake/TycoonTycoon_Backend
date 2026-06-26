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

        /// <summary>
        /// Initiates a password reset flow by sending a reset link to the admin's email.
        /// Returns true if email exists, false otherwise (for security, doesn't leak email existence).
        /// </summary>
        Task<bool> AdminInitiatePasswordResetAsync(string email, string ipAddress, string userAgent, CancellationToken ct = default);

        /// <summary>
        /// Validates and resets an admin's password using a valid reset token.
        /// Throws <see cref="InvalidOperationException"/> if token is invalid or expired.
        /// </summary>
        Task AdminResetPasswordAsync(string token, string newPassword, CancellationToken ct = default);

        /// <summary>
        /// Validates a password reset token without consuming it.
        /// Returns the associated email if valid, null otherwise.
        /// </summary>
        Task<string?> AdminValidateResetTokenAsync(string token, CancellationToken ct = default);
    }

    public record AuthResult(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        UserDto User
    );
}
