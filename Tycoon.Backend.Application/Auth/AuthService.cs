using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Auth
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(string email, string password, string deviceId, CancellationToken ct = default);
        Task<LoginResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);
        Task LogoutAsync(Guid userId, string deviceId, CancellationToken ct = default);
        Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    }

    public class AuthService : IAuthService
    {
        private readonly IAppDb _db;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;

        public AuthService(IAppDb db, IJwtService jwtService, IOptions<JwtSettings> jwtSettings)
        {
            _db = db;
            _jwtService = jwtService;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<LoginResponse> LoginAsync(string email, string password, string deviceId, CancellationToken ct = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);
            if (user == null || !user.VerifyPassword(password))
                throw new UnauthorizedAccessException("Invalid credentials");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is inactive");

            user.RecordLogin();

            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Handle);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var refreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

            user.AddRefreshToken(refreshToken, deviceId, refreshTokenExpiry);
            await _db.SaveChangesAsync(ct);

            return new LoginResponse(
                accessToken,
                refreshToken,
                _jwtSettings.AccessTokenExpirationMinutes * 60,
                new UserDto(user.Id, user.Email, user.Handle, user.Country, user.AvatarUrl, user.CreatedAt)
            );
        }

        public async Task<LoginResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
        {
            var storedToken = await _db.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken, ct);

            if (storedToken == null || !storedToken.IsValid())
                throw new UnauthorizedAccessException("Invalid or expired refresh token");

            var user = storedToken.User;
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Handle);
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var newRefreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

            storedToken.Revoke();
            user.AddRefreshToken(newRefreshToken, storedToken.DeviceId, newRefreshTokenExpiry);
            await _db.SaveChangesAsync(ct);

            return new LoginResponse(
                newAccessToken,
                newRefreshToken,
                _jwtSettings.AccessTokenExpirationMinutes * 60,
                new UserDto(user.Id, user.Email, user.Handle, user.Country, user.AvatarUrl, user.CreatedAt)
            );
        }

        public async Task LogoutAsync(Guid userId, string deviceId, CancellationToken ct = default)
        {
            var user = await _db.Users.FindAsync(new object[] { userId }, ct);
            if (user != null)
            {
                user.RevokeRefreshToken(deviceId);
                await _db.SaveChangesAsync(ct);
            }
        }

        public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);
            if (existingUser != null)
                throw new InvalidOperationException("User already exists");

            var user = new User(request.Email, request.Handle, request.Password, request.Country);
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            return new UserDto(user.Id, user.Email, user.Handle, user.Country, user.AvatarUrl, user.CreatedAt);
        }
    }
}
