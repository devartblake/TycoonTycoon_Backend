using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Auth
{
    public sealed class AuthService : IAuthService
    {
        private readonly IAppDb _database;
        private readonly JwtSettings _jwt;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IAppDb database, IOptions<JwtSettings> jwtOptions, ILogger<AuthService> logger)
        {
            _database = database;
            _jwt = jwtOptions.Value;
            _logger = logger;
        }

        // ── Public surface ────────────────────────────────────────────────────

        public Task<AuthResult> LoginAsync(string email, string password, string deviceId)
            => LoginInternalAsync(email, password, deviceId, clientType: "user");

        public Task<AuthResult> RefreshAsync(string refreshTokenString)
            => RefreshInternalAsync(refreshTokenString, expectedClientType: "user");

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

        // ── Private core logic ────────────────────────────────────────────────

        private async Task<AuthResult> LoginInternalAsync(
            string email, string password, string deviceId, string clientType)
        {
            var normalizedEmail = email.ToLowerInvariant();

            var authenticatedUser = await _database.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (authenticatedUser == null)
                throw new UnauthorizedAccessException("Authentication failed");

            if (!ValidatePasswordHash(password, authenticatedUser.PasswordHash))
                throw new UnauthorizedAccessException("Authentication failed");

            if (!authenticatedUser.IsActive)
                throw new UnauthorizedAccessException("User account is not active");

            authenticatedUser.RecordLogin();

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

            return BuildAuthResult(authenticatedUser, jwtToken, deviceRefreshToken);
        }

        private async Task<AuthResult> RefreshInternalAsync(
            string refreshTokenString, string expectedClientType)
        {
            var storedToken = await _database.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenString && !rt.IsRevoked);

            var currentTime = DateTimeOffset.UtcNow;
            if (storedToken == null || storedToken.ExpiresAt < currentTime)
                throw new UnauthorizedAccessException("Token refresh failed");

            if (!string.Equals(storedToken.ClientType, expectedClientType, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Token refresh failed");

            var tokenOwner = await _database.Users
                .FirstOrDefaultAsync(u => u.Id == storedToken.UserId);

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

            return BuildAuthResult(tokenOwner, newJwtToken, newDeviceToken);
        }

        // ── Token helpers ─────────────────────────────────────────────────────

        private string CreateJwtToken(User user, string clientType, AdminRole? aclRole = null)
        {
            var isAdmin = clientType.Equals("admin", StringComparison.OrdinalIgnoreCase);

            var scopes = isAdmin
                ? "users:read users:write questions:read questions:write events:read events:write notifications:write config:write"
                : "profile:read profile:write gameplay:read gameplay:write";

            // Super admins get ACL management scope
            if (isAdmin && aclRole == AdminRole.SuperAdmin)
                scopes += " acl:write";

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

        private AuthResult BuildAuthResult(User user, string jwtToken, string refreshToken)
        {
            var userProfile = new UserDto(
                user.Id,
                user.Handle,
                user.Email,
                user.Country,
                user.AvatarUrl,
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
