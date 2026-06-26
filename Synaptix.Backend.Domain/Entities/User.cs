using Synaptix.Backend.Domain.Primitives;

namespace Synaptix.Backend.Domain.Entities
{
    public class User : Entity
    {
        public new Guid Id { get; private set; }
        public string Email { get; private set; }
        public string Handle { get; private set; }
        public string PasswordHash { get; private set; }
        public string? Country { get; private set; }
        public string? AvatarUrl { get; private set; }
        public string? Tier { get; private set; }
        public int Mmr { get; private set; }
        public Dictionary<string, object> Flags { get; private set; }
        public string? SystemRole { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? LastLoginAt { get; private set; }
        public bool IsActive { get; private set; }

        /// <summary>
        /// True for device-first guest accounts created via /auth/device/bootstrap
        /// that have not yet been upgraded to a full email account. Such accounts
        /// have a synthetic, non-deliverable email until <see cref="UpgradeToFullAccount"/>.
        /// </summary>
        public bool IsAnonymous { get; private set; }

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

        /// <summary>
        /// Creates a device-first guest account. The email is synthetic and
        /// non-deliverable; the caller supplies a generated handle and a random
        /// password hash (guests authenticate by device, not credentials).
        /// </summary>
        public static User CreateGuest(string handle, string passwordHash, string? country = null)
        {
            var guest = new User($"guest_{Guid.NewGuid():N}@guest.local", handle, passwordHash, country)
            {
                IsAnonymous = true
            };
            return guest;
        }

        /// <summary>
        /// Promotes a guest account to a full email account, replacing the
        /// synthetic email/handle/credentials. Idempotency is the caller's
        /// responsibility (check <see cref="IsAnonymous"/> first).
        /// </summary>
        public void UpgradeToFullAccount(string email, string handle, string passwordHash)
        {
            Email = email.ToLowerInvariant();
            Handle = handle;
            PasswordHash = passwordHash;
            IsAnonymous = false;
        }

        public void UpdateProfile(string? handle, string? country, string? avatarUrl = null)
        {
            if (!string.IsNullOrWhiteSpace(handle)) Handle = handle;
            if (!string.IsNullOrWhiteSpace(country)) Country = country;
            if (avatarUrl is not null) AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        }

        public void RecordLogin()
        {
            LastLoginAt = DateTimeOffset.UtcNow;
        }

        public void SetActive(bool isActive)
        {
            IsActive = isActive;
        }

        public void SetSystemRole(string? role)
        {
            SystemRole = string.IsNullOrWhiteSpace(role) ? null : role.Trim().ToLowerInvariant();
        }

        public void Anonymize(string anonEmail, string anonHandle, string deadPasswordHash)
        {
            Email = anonEmail;
            Handle = anonHandle;
            PasswordHash = deadPasswordHash;
            Country = null;
            AvatarUrl = null;
            IsActive = false;
        }

        public void ChangePassword(string newPasswordHash)
        {
            PasswordHash = newPasswordHash;
        }
    }
}
