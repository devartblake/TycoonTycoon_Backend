using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Email;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Auth
{
    public sealed class AuthService : IAuthService
    {
        private readonly IAppDb _database;
        private readonly JwtSettings _jwt;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;

        private sealed record AuthUserSnapshot(
            Guid Id,
            string Email,
            string Handle,
            string PasswordHash,
            string? Country,
            string? Tier,
            int Mmr,
            bool IsActive);

        public AuthService(
            IAppDb database,
            IOptions<JwtSettings> jwtOptions,
            ILogger<AuthService> logger,
            IEmailService emailService)
        {
            _database = database;
            _jwt = jwtOptions.Value;
            _logger = logger;
            _emailService = emailService;
        }

        // ── Public surface ────────────────────────────────────────────────────

        public Task<AuthResult> LoginAsync(string email, string password, string deviceId)
            => LoginInternalAsync(email, password, deviceId, clientType: "user");

        public Task<AuthResult> RefreshAsync(string refreshTokenString, Guid? expectedSubject = null)
            => RefreshInternalAsync(refreshTokenString, expectedClientType: "user", expectedSubject: expectedSubject);

        public Task<AuthResult> AdminLoginAsync(string email, string password, string deviceId)
            => LoginInternalAsync(email, password, deviceId, clientType: "admin");

        public Task<AuthResult> AdminRefreshAsync(string refreshToken)
            => RefreshInternalAsync(refreshToken, expectedClientType: "admin");

        public async Task LogoutAsync(string deviceId, Guid userId)
        {
            var activeTokensForDevice = await _database.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.DeviceId == deviceId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in activeTokensForDevice)
                token.Revoke();

            if (activeTokensForDevice.Any())
                await _database.SaveChangesAsync();
        }

        public async Task<User> RegisterAsync(string email, string password, string handle, string? country = null)
        {
            var normalizedEmail = email.ToLowerInvariant();

            var emailConflict = await _database.Users
                .AnyAsync(u => u.Email == normalizedEmail);
            if (emailConflict)
                throw new InvalidOperationException("This email is already in use");

            var handleConflict = await _database.Users
                .AnyAsync(u => u.Handle == handle);
            if (handleConflict)
                throw new InvalidOperationException("This handle is not available");

            var securePasswordHash = ComputePasswordHash(password);
            var newUser = new User(email, handle, securePasswordHash, country);

            _database.Users.Add(newUser);
            await _database.SaveChangesAsync();

            return newUser;
        }

        public async Task<AuthResult> BootstrapDeviceAsync(string deviceId, string? displayName = null)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new InvalidOperationException("deviceId is required");

            // Generate a collision-resistant handle; guests never log in by handle.
            var handle = $"guest_{Guid.NewGuid():N}"[..16];
            var randomSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var guest = User.CreateGuest(handle, ComputePasswordHash(randomSecret));

            _database.Users.Add(guest);

            var snapshot = BuildSnapshot(guest);
            var jwtToken = CreateJwtToken(snapshot, clientType: "user");
            var refreshToken = CreateRefreshTokenForDevice(guest.Id, deviceId, clientType: "user");

            await _database.SaveChangesAsync();

            return BuildAuthResult(snapshot, jwtToken, refreshToken);
        }

        public async Task<AuthResult> UpgradeAccountAsync(
            Guid userId, string email, string password, string deviceId,
            string? handle = null, string? country = null)
        {
            var user = await _database.Users.FindAsync(userId);
            if (user is null)
                throw new UnauthorizedAccessException("Account not found");

            if (!user.IsAnonymous)
                throw new InvalidOperationException("This account is already registered");

            var normalizedEmail = email.ToLowerInvariant();
            var resolvedHandle = !string.IsNullOrWhiteSpace(handle) ? handle : email.Split('@')[0];

            var emailConflict = await _database.Users
                .AnyAsync(u => u.Email == normalizedEmail && u.Id != userId);
            if (emailConflict)
                throw new InvalidOperationException("This email is already in use");

            var handleConflict = await _database.Users
                .AnyAsync(u => u.Handle == resolvedHandle && u.Id != userId);
            if (handleConflict)
                throw new InvalidOperationException("This handle is not available");

            user.UpgradeToFullAccount(normalizedEmail, resolvedHandle, ComputePasswordHash(password));
            if (!string.IsNullOrWhiteSpace(country))
                user.UpdateProfile(handle: null, country: country);

            // Rotate device tokens: revoke the guest device session, issue a fresh one.
            var activeTokens = await _database.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.DeviceId == deviceId && !rt.IsRevoked)
                .ToListAsync();
            foreach (var token in activeTokens)
                token.Revoke();

            var snapshot = BuildSnapshot(user);
            var jwtToken = CreateJwtToken(snapshot, clientType: "user");
            var refreshToken = CreateRefreshTokenForDevice(user.Id, deviceId, clientType: "user");

            await _database.SaveChangesAsync();

            return BuildAuthResult(snapshot, jwtToken, refreshToken);
        }

        public async Task<bool> AdminInitiatePasswordResetAsync(
            string email, string ipAddress, string userAgent, CancellationToken ct = default)
        {
            var normalizedEmail = email.ToLowerInvariant();

            // Check if user exists (don't reveal if they don't for security)
            var user = await _database.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

            if (user is null)
                return false;

            // Revoke all existing unused reset tokens for this user
            var existingTokens = await _database.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.Used)
                .ToListAsync(ct);

            foreach (var token in existingTokens)
                token.MarkAsUsed();

            // Create new reset token (15 minute expiry)
            var resetToken = new PasswordResetToken(
                user.Id,
                Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                DateTimeOffset.UtcNow.AddMinutes(15),
                ipAddress,
                userAgent
            );

            _database.PasswordResetTokens.Add(resetToken);
            await _database.SaveChangesAsync(ct);

            // Send password reset email
            var resetUrl = $"https://admin.synaptixplay.com/reset-password?token={Uri.EscapeDataString(resetToken.Token)}";
            var subject = "Synaptix Admin - Password Reset Request";
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #1a5f7a; color: white; padding: 20px; border-radius: 4px 4px 0 0; }}
        .content {{ background-color: #f5f5f5; padding: 20px; border-radius: 0 0 4px 4px; }}
        .button {{ display: inline-block; background-color: #1a5f7a; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
        .footer {{ font-size: 12px; color: #666; margin-top: 20px; }}
        .warning {{ background-color: #fff3cd; padding: 10px; border-radius: 4px; margin: 10px 0; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Password Reset Request</h1>
        </div>
        <div class=""content"">
            <p>Hello,</p>
            <p>We received a request to reset the password for your Synaptix Admin account. If you didn't make this request, you can safely ignore this email.</p>
            <p>To reset your password, click the button below. This link will expire in 15 minutes.</p>
            <a href=""{resetUrl}"" class=""button"">Reset Password</a>
            <p>Or copy this link in your browser:</p>
            <p style=""word-break: break-all; background-color: white; padding: 10px; border-radius: 4px; font-family: monospace; font-size: 12px;"">{resetUrl}</p>
            <div class=""warning"">
                <strong>Security Notice:</strong> Never share this link with anyone. It will automatically expire after 15 minutes for security reasons.
            </div>
        </div>
        <div class=""footer"">
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";

            try
            {
                await _emailService.SendAsync(user.Email, subject, htmlBody, ct);
                _logger.LogInformation("Password reset email sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                // Don't throw - we've already saved the token
            }

            return true;
        }

        public async Task AdminResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
                throw new InvalidOperationException("Token and password are required");

            // Validate password strength (min 8 chars for admin accounts, could be more strict)
            if (newPassword.Length < 8)
                throw new InvalidOperationException("Password must be at least 8 characters long");

            var resetToken = await _database.PasswordResetTokens
                .Include(t => t.UserId)
                .FirstOrDefaultAsync(t => t.Token == token && !t.Used, ct);

            if (resetToken is null || !resetToken.IsValid())
                throw new InvalidOperationException("Invalid or expired password reset token");

            var user = await _database.Users.FindAsync(new object[] { resetToken.UserId }, cancellationToken: ct);
            if (user is null)
                throw new InvalidOperationException("User not found");

            // Update password
            var newPasswordHash = ComputePasswordHash(newPassword);
            user.ChangePassword(newPasswordHash);
            resetToken.MarkAsUsed();

            // Revoke all active refresh tokens for this user (force re-login)
            var activeTokens = await _database.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync(ct);

            foreach (var refreshToken in activeTokens)
                refreshToken.Revoke();

            await _database.SaveChangesAsync(ct);

            _logger.LogInformation("Password reset completed for {Email}", user.Email);
        }

        public async Task<string?> AdminValidateResetTokenAsync(string token, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var resetToken = await _database.PasswordResetTokens
                .Include(t => t.UserId)
                .FirstOrDefaultAsync(t => t.Token == token && !t.Used, ct);

            if (resetToken is null || !resetToken.IsValid())
                return null;

            var user = await _database.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == resetToken.UserId, ct);

            return user?.Email;
        }

        private static AuthUserSnapshot BuildSnapshot(User user) => new(
            user.Id,
            user.Email,
            user.Handle,
            user.PasswordHash,
            user.Country,
            user.Tier,
            user.Mmr,
            user.IsActive);

        // ── Private core logic ────────────────────────────────────────────────

        private async Task<AuthResult> LoginInternalAsync(
            string email, string password, string deviceId, string clientType)
        {
            var normalizedEmail = email.ToLowerInvariant();

            var authenticatedUser = await _database.Users
                .AsNoTracking()
                .Where(u => u.Email == normalizedEmail)
                .Select(u => new AuthUserSnapshot(
                    u.Id,
                    u.Email,
                    u.Handle,
                    u.PasswordHash,
                    u.Country,
                    u.Tier,
                    u.Mmr,
                    u.IsActive))
                .FirstOrDefaultAsync();

            if (authenticatedUser == null)
                throw new UnauthorizedAccessException("Authentication failed");

            if (!ValidatePasswordHash(password, authenticatedUser.PasswordHash))
                throw new UnauthorizedAccessException("Authentication failed");

            if (!authenticatedUser.IsActive)
                throw new UnauthorizedAccessException("User account is not active");

            var userEntity = await _database.Users.FindAsync(authenticatedUser.Id);
            userEntity?.RecordLogin();
            // Note: SaveChangesAsync below persists this login timestamp together with the refresh token.

            // Look up ACL role for admin logins to grant elevated scopes
            AdminRole? aclRole = null;
            if (clientType.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var aclEntry = await _database.AdminEmailAcls.AsNoTracking()
                        .FirstOrDefaultAsync(e => e.NormalizedEmail == normalizedEmail && e.ListType == AdminAclListType.Allow);
                    aclRole = aclEntry?.Role;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AdminEmailAcls query failed (table may not exist yet). Proceeding without ACL role for {Email}.", normalizedEmail);
                }
            }

            var jwtToken = CreateJwtToken(authenticatedUser, clientType, aclRole);
            var deviceRefreshToken = CreateRefreshTokenForDevice(
                authenticatedUser.Id, deviceId, clientType);

            await _database.SaveChangesAsync();

            return BuildAuthResult(authenticatedUser, jwtToken, refreshToken: deviceRefreshToken);
        }

        // OAuth2 refresh-token reuse detection: a rotated (already-revoked) token
        // presented again is treated as replay. A short grace window absorbs benign
        // client retries (a client that got a network drop after we rotated but
        // before it stored the new token); reuse outside the window is treated as a
        // compromised family and every active token for that user+device is revoked.
        private static readonly TimeSpan RefreshReuseGraceWindow = TimeSpan.FromSeconds(30);

        private async Task<AuthResult> RefreshInternalAsync(
            string refreshTokenString, string expectedClientType, Guid? expectedSubject = null)
        {
            // Match regardless of revoked state so we can distinguish "unknown token"
            // from "known but already-rotated token" (reuse).
            var storedToken = await _database.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenString);

            var currentTime = DateTimeOffset.UtcNow;
            if (storedToken == null)
                throw new UnauthorizedAccessException("Token refresh failed");

            if (storedToken.IsRevoked)
            {
                var revokedAt = storedToken.RevokedAt ?? currentTime;
                var withinGrace = currentTime - revokedAt <= RefreshReuseGraceWindow;

                if (!withinGrace)
                {
                    // Reuse of a rotated token past the grace window => likely theft.
                    // Revoke the entire active family for this user+device.
                    var family = await _database.RefreshTokens
                        .Where(rt => rt.UserId == storedToken.UserId
                            && rt.DeviceId == storedToken.DeviceId
                            && !rt.IsRevoked)
                        .ToListAsync();

                    foreach (var token in family)
                        token.Revoke();

                    if (family.Count > 0)
                        await _database.SaveChangesAsync();

                    _logger.LogWarning(
                        "Refresh-token reuse detected for user {UserId} device {DeviceId}; revoked {Count} active token(s) in family.",
                        storedToken.UserId, storedToken.DeviceId, family.Count);
                }
                else
                {
                    _logger.LogInformation(
                        "Refresh-token retry within grace window for user {UserId} device {DeviceId}; not treating as reuse.",
                        storedToken.UserId, storedToken.DeviceId);
                }

                throw new UnauthorizedAccessException("Token refresh failed");
            }

            if (storedToken.ExpiresAt < currentTime)
                throw new UnauthorizedAccessException("Token refresh failed");

            if (!string.Equals(storedToken.ClientType, expectedClientType, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Token refresh failed");

            // Channel-subject binding: when the request arrived over a secure channel
            // bound to an authenticated subject, the refresh token must belong to that
            // same subject. Prevents replaying a stolen refresh token over an
            // attacker's own channel. Skipped for anonymous refresh (expectedSubject null).
            if (expectedSubject.HasValue && storedToken.UserId != expectedSubject.Value)
            {
                _logger.LogWarning(
                    "Refresh subject mismatch: token owner {TokenOwner} does not match channel subject {ChannelSubject}.",
                    storedToken.UserId, expectedSubject.Value);
                throw new UnauthorizedAccessException("Token refresh failed");
            }

            var tokenOwner = await _database.Users
                .AsNoTracking()
                .Where(u => u.Id == storedToken.UserId)
                .Select(u => new AuthUserSnapshot(
                    u.Id,
                    u.Email,
                    u.Handle,
                    u.PasswordHash,
                    u.Country,
                    u.Tier,
                    u.Mmr,
                    u.IsActive))
                .FirstOrDefaultAsync();

            if (tokenOwner == null || !tokenOwner.IsActive)
                throw new UnauthorizedAccessException("Token refresh failed");

            storedToken.Revoke();

            AdminRole? aclRole = null;
            if (expectedClientType.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var normalizedEmail = tokenOwner.Email.ToLowerInvariant();
                    var aclEntry = await _database.AdminEmailAcls.AsNoTracking()
                        .FirstOrDefaultAsync(e => e.NormalizedEmail == normalizedEmail && e.ListType == AdminAclListType.Allow);
                    aclRole = aclEntry?.Role;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AdminEmailAcls query failed during token refresh (table may not exist yet). Proceeding without ACL role.");
                }
            }

            var newJwtToken = CreateJwtToken(tokenOwner, expectedClientType, aclRole);
            var newDeviceToken = CreateRefreshTokenForDevice(
                tokenOwner.Id, storedToken.DeviceId, expectedClientType);

            await _database.SaveChangesAsync();

            return BuildAuthResult(tokenOwner, newJwtToken, refreshToken: newDeviceToken);
        }

        // ── Token helpers ─────────────────────────────────────────────────────

        private string CreateJwtToken(AuthUserSnapshot user, string clientType, AdminRole? aclRole = null)
        {
            var isAdmin = clientType.Equals("admin", StringComparison.OrdinalIgnoreCase);

            var adminProfile = isAdmin ? AdminPermissionProfiles.ForRole(aclRole ?? AdminRole.SuperAdmin) : null;
            var scopes = isAdmin
                ? adminProfile!.Scope
                : "profile:read profile:write gameplay:read gameplay:write";

            var role = isAdmin ? "admin" : "user";
            var audience = isAdmin ? "admin-app" : "mobile-app";

            var userClaims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new("handle",      user.Handle),
                new("role",        role),
                new("scope",       scopes),
                new("client_type", clientType),
                new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
            };

            if (adminProfile is not null)
                userClaims.Add(new("admin_role", adminProfile.Role.ToString()));

            var signingKeyBytes = Encoding.UTF8.GetBytes(_jwt.SecretKey);
            var securityKey = new SymmetricSecurityKey(signingKeyBytes);
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: audience,
                claims: userClaims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes),
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        private string CreateRefreshTokenForDevice(
            Guid userId, string deviceId, string clientType)
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            var tokenValue = Convert.ToBase64String(randomBytes);
            var expirationTime = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays);

            var deviceToken = new RefreshToken(userId, tokenValue, deviceId, expirationTime, clientType);
            _database.RefreshTokens.Add(deviceToken);

            return tokenValue;
        }

        // ── Password helpers ──────────────────────────────────────────────────

        private string ComputePasswordHash(string plainTextPassword)
            => BCrypt.Net.BCrypt.HashPassword(plainTextPassword);

        private bool ValidatePasswordHash(string plainTextPassword, string storedHash)
            => BCrypt.Net.BCrypt.Verify(plainTextPassword, storedHash);

        // ── Result builder ────────────────────────────────────────────────────

        private AuthResult BuildAuthResult(AuthUserSnapshot user, string jwtToken, string refreshToken)
        {
            var userProfile = new UserDto(
                user.Id,
                user.Handle,
                user.Email,
                user.Country,
                AvatarUrl: null,
                user.Tier,
                user.Mmr
            );

            return new AuthResult(
                jwtToken,
                refreshToken,
                _jwt.AccessTokenExpirationMinutes * 60,
                userProfile
            );
        }
    }
}
