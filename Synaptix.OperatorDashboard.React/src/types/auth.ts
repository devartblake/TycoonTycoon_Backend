/**
 * Authorization and authentication types
 */

export type Permission =
  | 'users:read'
  | 'users:write'
  | 'moderation:read'
  | 'moderation:write'
  | 'events:read'
  | 'events:write'
  | 'storage:read'
  | 'storage:write'
  | 'config:read'
  | 'config:write'
  | 'economy:read'
  | 'economy:write'
  | 'content:read'
  | 'content:write'
  | 'notifications:read'
  | 'notifications:write'
  | 'anti-cheat:read'
  | 'anti-cheat:write'
  | 'operations:read'
  | 'operations:write'
  | 'personalization:read'
  | 'personalization:write'
  | 'audit:read'

/**
 * Role definitions with associated permissions.
 * These are derived from Django's ADMIN_PROFILES context.
 */
export type Role =
  | 'super_admin'
  | 'admin'
  | 'moderator'
  | 'analyst'
  | 'viewer'

export const ROLE_PERMISSIONS: Record<Role, Permission[]> = {
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
}

export interface AdminProfile {
  email: string
  username?: string
  role?: Role
  permissions: Permission[]
  createdAt?: string
  lastLoginAt?: string
}

export interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  profile: AdminProfile | null
  isLoading: boolean
  error: string | null
  isAuthenticated: boolean
  expiresAt: number | null
}

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
  admin: AdminProfile
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export interface RefreshTokenResponse {
  accessToken: string
  expiresIn: number
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  token: string
  newPassword: string
  confirmPassword: string
}

export interface ValidateResetTokenRequest {
  token: string
}

export interface ValidateResetTokenResponse {
  valid: boolean
  expiresAt?: string
}
