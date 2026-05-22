using Synaptix.Backend.Domain.Primitives;

namespace Synaptix.Backend.Domain.Entities;

/// <summary>
/// Access-control entry for an admin email address.
/// Supports allow/block lists with role assignment.
/// Blocklist entries take precedence over allowlist entries.
/// </summary>
public sealed class AdminEmailAcl : Entity
{
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public AdminAclListType ListType { get; private set; }
    public AdminRole Role { get; private set; }
    public string? Notes { get; private set; }
    public string AddedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private AdminEmailAcl() { }

    public AdminEmailAcl(string email, AdminAclListType listType, AdminRole role, string addedBy, string? notes = null)
    {
        Id = Guid.NewGuid();
        Email = email.Trim();
        NormalizedEmail = email.Trim().ToLowerInvariant();
        ListType = listType;
        Role = role;
        AddedBy = addedBy;
        Notes = notes;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Update(AdminAclListType listType, AdminRole role, string? notes)
    {
        ListType = listType;
        Role = role;
        Notes = notes;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}

public enum AdminAclListType
{
    Allow = 0,
    Block = 1
}

public enum AdminRole
{
    Viewer = 0,
    Admin = 1,
    SuperAdmin = 2
}
