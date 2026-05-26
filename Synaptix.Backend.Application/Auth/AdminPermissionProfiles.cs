using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Application.Auth;

public sealed record AdminPermissionProfile(
    AdminRole Role,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions)
{
    public string Scope => string.Join(' ', Permissions);
}

public static class AdminPermissionProfiles
{
    private static readonly string[] ViewerPermissions =
    [
        "users:read",
        "moderation:read",
        "questions:read",
        "events:read",
        "store:read",
        "economy:read",
        "anticheat:read",
        "notifications:read",
        "seasons:read",
        "eventqueue:read",
        "personalization:read",
    ];

    private static readonly string[] ModeratorPermissions =
    [
        .. ViewerPermissions,
        "users:write",
        "moderation:write",
        "questions:write",
        "events:write",
        "anticheat:write",
    ];

    private static readonly string[] AdminPermissions =
    [
        .. ModeratorPermissions,
        "store:write",
        "economy:write",
        "notifications:write",
        "seasons:write",
        "eventqueue:write",
        "personalization:write",
        "config:write",
    ];

    private static readonly string[] SuperAdminPermissions =
    [
        .. AdminPermissions,
        "acl:write",
    ];

    public static AdminPermissionProfile ForRole(AdminRole role)
    {
        var permissions = role switch
        {
            AdminRole.Viewer => ViewerPermissions,
            AdminRole.Moderator => ModeratorPermissions,
            AdminRole.Admin => AdminPermissions,
            AdminRole.SuperAdmin => SuperAdminPermissions,
            _ => ViewerPermissions,
        };

        return new AdminPermissionProfile(
            role,
            ["admin", role.ToString().ToLowerInvariant()],
            permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }
}
