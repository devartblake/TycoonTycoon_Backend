using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Entities
{
    public class RefreshToken : Entity
    {
        public new Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string Token { get; private set; }
        public string DeviceId { get; private set; }
        public DateTimeOffset ExpiresAt { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public bool IsRevoked { get; private set; }
        public DateTimeOffset? RevokedAt { get; private set; }

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
            ExpiresAt = expiresAt;
            CreatedAt = DateTimeOffset.UtcNow;
            IsRevoked = false;
        }

        public void Revoke()
        {
            IsRevoked = true;
            RevokedAt = DateTimeOffset.UtcNow;
        }
    }
}
