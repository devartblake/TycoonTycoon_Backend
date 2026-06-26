namespace Synaptix.Backend.Domain.Entities
{
    /// <summary>
    /// OTP token for password reset and email verification
    /// </summary>
    public class OtpToken
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string OtpHash { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public int VerificationAttempts { get; set; }

        public OtpToken()
        {
        }

        public OtpToken(string email, string otpHash, DateTimeOffset expiresAt)
        {
            Id = Guid.NewGuid();
            Email = email.ToLowerInvariant();
            OtpHash = otpHash;
            CreatedAt = DateTimeOffset.UtcNow;
            ExpiresAt = expiresAt;
            IsUsed = false;
            VerificationAttempts = 0;
        }

        public bool IsValid => !IsUsed && DateTimeOffset.UtcNow < ExpiresAt;

        public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
    }
}
