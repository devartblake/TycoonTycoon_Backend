using Synaptix.Backend.Domain.Primitives;

namespace Synaptix.Backend.Domain.Entities
{
    public class PasswordResetToken : Entity
    {
        public Guid UserId { get; private set; }
        public string Token { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset ExpiresAt { get; private set; }
        public bool Used { get; private set; }
        public string? IpAddress { get; private set; }
        public string? UserAgent { get; private set; }

        private PasswordResetToken()
        {
            Token = string.Empty;
        }

        public PasswordResetToken(Guid userId, string token, DateTimeOffset expiresAt, string? ipAddress = null, string? userAgent = null)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Token = token;
            CreatedAt = DateTimeOffset.UtcNow;
            ExpiresAt = expiresAt;
            Used = false;
            IpAddress = ipAddress;
            UserAgent = userAgent;
        }

        public bool IsValid() => !Used && DateTimeOffset.UtcNow < ExpiresAt;

        public void MarkAsUsed()
        {
            Used = true;
        }
    }
}
