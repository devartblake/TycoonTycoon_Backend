using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
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

        public AuthService(IAppDb database, IOptions<JwtSettings> jwtOptions)
        {
            _database = database;
            _jwt = jwtOptions.Value;
        }

        public Task<AuthResult> LoginAsync(string email, string password, string deviceId)
            => LoginInternalAsync(email, password, deviceId, clientType: "user");

        public Task<AuthResult> RefreshAsync(string refreshTokenString)
            => RefreshInternalAsync(refreshTokenString, expectedClientType: "user");

        public Task<AuthResult> AdminLoginAsync(string email, string password, string deviceId)
            => LoginInternalAsync(email, password, deviceId, clientType: "admin");

        public Task<AuthResult> AdminRefreshAsync(string refreshToken)
            => RefreshInternalAsync(refreshToken, expectedClientType: "admin");

        private async Task<AuthResult> LoginInternalAsync(string email, string password, string deviceId, string clientType)
            var jwtToken = CreateJwtToken(authenticatedUser, clientType);
            var deviceRefreshToken = await CreateRefreshTokenForDevice(authenticatedUser.Id, deviceId, clientType);
        private async Task<AuthResult> RefreshInternalAsync(string refreshTokenString, string expectedClientType)
            if (!string.Equals(storedToken.ClientType, expectedClientType, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Token refresh failed");

            var newJwtToken = CreateJwtToken(tokenOwner, expectedClientType);
            var newDeviceToken = await CreateRefreshTokenForDevice(tokenOwner.Id, storedToken.DeviceId, expectedClientType);
        private string CreateJwtToken(User user, string clientType)
        {
            var scopes = clientType.Equals("admin", StringComparison.OrdinalIgnoreCase)
                ? "users:read users:write questions:read questions:write events:read events:write notifications:write config:write"
                : "profile:read profile:write gameplay:read gameplay:write";

            var role = clientType.Equals("admin", StringComparison.OrdinalIgnoreCase) ? "admin" : "user";
            var audience = clientType.Equals("admin", StringComparison.OrdinalIgnoreCase) ? "admin-app" : "mobile-app";

                new("role", role),
                new("scope", scopes),
                new("client_type", clientType),
                new("aud", audience),
        private async Task<string> CreateRefreshTokenForDevice(Guid userId, string deviceId, string clientType)
            var deviceToken = new RefreshToken(userId, tokenValue, deviceId, expirationTime, clientType);
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (authenticatedUser == null)
                throw new UnauthorizedAccessException("Authentication failed");

            if (!ValidatePasswordHash(password, authenticatedUser.PasswordHash))
                throw new UnauthorizedAccessException("Authentication failed");

            if (!authenticatedUser.IsActive)
                throw new UnauthorizedAccessException("User account is not active");

            authenticatedUser.RecordLogin();
            await _database.SaveChangesAsync();

            var jwtToken = CreateJwtToken(authenticatedUser);
            var deviceRefreshToken = await CreateRefreshTokenForDevice(authenticatedUser.Id, deviceId);

            return BuildAuthResult(authenticatedUser, jwtToken, deviceRefreshToken);
        }

        public async Task<AuthResult> RefreshAsync(string refreshTokenString)
        {
            var storedToken = await _database.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenString && !rt.IsRevoked);

            var currentTime = DateTimeOffset.UtcNow;
            if (storedToken == null || storedToken.ExpiresAt < currentTime)
                throw new UnauthorizedAccessException("Token refresh failed");

            var tokenOwner = await _database.Users
                .FirstOrDefaultAsync(u => u.Id == storedToken.UserId);

            if (tokenOwner == null || !tokenOwner.IsActive)
                throw new UnauthorizedAccessException("Token refresh failed");

            storedToken.Revoke();

            var newJwtToken = CreateJwtToken(tokenOwner);
            var newDeviceToken = await CreateRefreshTokenForDevice(tokenOwner.Id, storedToken.DeviceId);

            await _database.SaveChangesAsync();

            return BuildAuthResult(tokenOwner, newJwtToken, newDeviceToken);
        }

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

        private string CreateJwtToken(User user)
        {
            var userClaims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new("handle", user.Handle),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var signingKeyBytes = Encoding.UTF8.GetBytes(JwtSigningKey);
            var securityKey = new SymmetricSecurityKey(signingKeyBytes);
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: TokenIssuerName,
                audience: TokenIssuerName,
                claims: userClaims,
                expires: DateTime.UtcNow.AddMinutes(AccessTokenLifetime),
                signingCredentials: signingCredentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(tokenDescriptor);
        }

        private async Task<string> CreateRefreshTokenForDevice(Guid userId, string deviceId)
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            var tokenValue = Convert.ToBase64String(randomBytes);
            var expirationTime = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays);

            var deviceToken = new RefreshToken(userId, tokenValue, deviceId, expirationTime);
            _database.RefreshTokens.Add(deviceToken);

            return tokenValue;
        }

        private string ComputePasswordHash(string plainTextPassword)
        {
            return BCrypt.Net.BCrypt.HashPassword(plainTextPassword);
        }

        private bool ValidatePasswordHash(string plainTextPassword, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, storedHash);
        }

        private AuthResult BuildAuthResult(User user, string jwtToken, string refreshToken)
        {
            var userProfile = new UserDto(
                user.Id,
                user.Handle,
                user.Email,
                user.Country,
                user.Tier,
                user.Mmr
            );

            return new AuthResult(
                jwtToken,
                refreshToken,
                AccessTokenLifetime * 60,
                userProfile
            );
        }
    }
}