using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Entities
{
    public class User : Entity
    {
        public new Guid Id { get; private set; }
        public string Email { get; private set; }
        public string Handle { get; private set; }
        public string PasswordHash { get; private set; }
        public string Country { get; private set; }
        public string? AvatarUrl { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? LastLoginAt { get; private set; }
        public bool IsActive { get; private set; }
        
        // Refresh tokens
        public List<RefreshToken> RefreshTokens { get; private set; }

        // EF Core constructor
        private User() 
        { 
            Email = string.Empty;
            Handle = string.Empty;
            PasswordHash = string.Empty;
            Country = string.Empty;
            RefreshTokens = new List<RefreshToken>();
        }

        public User(string email, string handle, string password, string country)
        {
            Id = Guid.NewGuid();
            Email = email.ToLowerInvariant();
            Handle = handle;
            PasswordHash = HashPassword(password);
            Country = country;
            CreatedAt = DateTimeOffset.UtcNow;
            IsActive = true;
            RefreshTokens = new List<RefreshToken>();
        }

        public void UpdateProfile(string handle, string country)
        {
            Handle = handle;
            Country = country;
        }

        public void RecordLogin()
        {
            LastLoginAt = DateTimeOffset.UtcNow;
        }

        public bool VerifyPassword(string password)
        {
            return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
        }

        public void AddRefreshToken(string token, string deviceId, DateTimeOffset expiresAt)
        {
            RefreshTokens.Add(new RefreshToken(Id, token, deviceId, expiresAt));
        }

        public void RevokeRefreshToken(string deviceId)
        {
            var tokensToRevoke = RefreshTokens.Where(rt => rt.DeviceId == deviceId).ToList();
            foreach (var token in tokensToRevoke)
            {
                token.Revoke();
            }
        }

        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }
    }

    public class RefreshToken
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string Token { get; private set; }
        public string DeviceId { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset ExpiresAt { get; private set; }
        public bool IsRevoked { get; private set; }

        // Navigation property
        public User? User { get; private set; }

        // EF Core constructor
        private RefreshToken() 
        { 
            Token = string.Empty;
            DeviceId = string.Empty;
        }

        public RefreshToken(Guid userId, string token, string deviceId, DateTimeOffset expiresAt)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Token = token;
            DeviceId = deviceId;
            CreatedAt = DateTimeOffset.UtcNow;
            ExpiresAt = expiresAt;
            IsRevoked = false;
        }

        public void Revoke() => IsRevoked = true;
        public bool IsValid() => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow;
    }
}
