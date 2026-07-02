/**
 * Authorization and authentication types
 */
export const ROLE_PERMISSIONS = {
    super_admin: [
        // Full access
        'users:read', 'users:write',
        'moderation:read', 'moderation:write',
        'events:read', 'events:write',
        'storage:read', 'storage:write',
        'config:read', 'config:write',
        'economy:read', 'economy:write',
        'content:read', 'content:write',
        'notifications:read', 'notifications:write',
        'anti-cheat:read', 'anti-cheat:write',
        'operations:read', 'operations:write',
        'personalization:read', 'personalization:write',
        'audit:read',
    ],
    admin: [
        // Administrative access (read/write for most, no config)
        'users:read', 'users:write',
        'moderation:read', 'moderation:write',
        'events:read', 'events:write',
        'storage:read', 'storage:write',
        'economy:read', 'economy:write',
        'content:read', 'content:write',
        'notifications:read', 'notifications:write',
        'anti-cheat:read', 'anti-cheat:write',
        'operations:read', 'operations:write',
        'personalization:read', 'personalization:write',
        'audit:read',
    ],
    moderator: [
        // Moderation focus
        'users:read', 'users:write',
        'moderation:read', 'moderation:write',
        'anti-cheat:read', 'anti-cheat:write',
        'personalization:read',
        'audit:read',
    ],
    analyst: [
        // Read-only with some write for events
        'users:read',
        'moderation:read',
        'events:read',
        'economy:read',
        'content:read',
        'notifications:read',
        'personalization:read',
        'audit:read',
    ],
    viewer: [
        // Read-only across the board
        'users:read',
        'moderation:read',
        'events:read',
        'economy:read',
        'content:read',
        'notifications:read',
        'personalization:read',
        'audit:read',
    ],
};
