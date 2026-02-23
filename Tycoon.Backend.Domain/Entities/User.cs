using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Entities
{
    public class User : Entity
    {
        public new Guid Id { get; private set; }
        public string Email { get; private set; }
        public string Handle { get; private set; }
        public string PasswordHash { get; private set; }
        public string? Country { get; private set; }
        public string? Tier { get; private set; }
        public int Mmr { get; private set; }
        public Dictionary<string, object> Flags { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? LastLoginAt { get; private set; }
        public bool IsActive { get; private set; }

        private User() 
        { 
            Email = string.Empty;
            Handle = string.Empty;
            PasswordHash = string.Empty;
            Flags = new Dictionary<string, object>();
        }

        public User(string email, string handle, string passwordHash, string? country = null)
        {
            Id = Guid.NewGuid();
            Email = email.ToLowerInvariant();
            Handle = handle;
            PasswordHash = passwordHash;
            Country = country;
            Tier = "T1";
            Mmr = 1000;
            Flags = new Dictionary<string, object>();
            CreatedAt = DateTimeOffset.UtcNow;
            IsActive = true;
        }

        public void UpdateProfile(string? handle, string? country)
        {
            if (!string.IsNullOrWhiteSpace(handle)) Handle = handle;
            if (!string.IsNullOrWhiteSpace(country)) Country = country;
        }

        public void RecordLogin()
        {
            LastLoginAt = DateTimeOffset.UtcNow;
        }

        public void SetActive(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
