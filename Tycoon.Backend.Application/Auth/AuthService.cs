using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IAppDb _database;
        private readonly IConfiguration _configuration;
        private const int DefaultTokenExpiryMinutes = 15;
        private const int RefreshTokenLifetimeDays = 30;

        public AuthService(IAppDb database, IConfiguration configuration)
        {
            _database = database;
            _configuration = configuration;
        }

        private string JwtSigningKey => _configuration["Jwt:Secret"] 
            ?? throw new InvalidOperationException("JWT signing key must be configured");
        
        private string TokenIssuerName => _configuration["Jwt:Issuer"] ?? "TycoonBackend";
        
        private int AccessTokenLifetime => int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var mins) 
            ? mins : DefaultTokenExpiryMinutes;

        public async Task<AuthResult> LoginAsync(string email, string password, string deviceId)
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
            var expirationTime = DateTimeOffset.UtcNow.AddDays(RefreshTokenLifetimeDays);

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
