/**
 * RBAC hooks for permission checking
 * Mirrors Django's require_permission decorator
 */

import { useAuthStore } from '@/features/auth/store'
import type { Permission } from '@/types/auth'

/**
 * Check if user has a specific permission
 * @param permission The permission to check
 * @returns true if user has the permission
 */
export function usePermission(permission: Permission): boolean {
  return useAuthStore((state) => state.hasPermission(permission))
}

/**
 * Check if user has any of the specified permissions
 * @param permissions Array of permissions
 * @returns true if user has any of the permissions
 */
export function useAnyPermission(permissions: Permission[]): boolean {
  return useAuthStore((state) => state.hasAnyPermission(permissions))
}

/**
 * Check if user has all of the specified permissions
 * @param permissions Array of permissions
 * @returns true if user has all permissions
 */
export function useAllPermissions(permissions: Permission[]): boolean {
  return useAuthStore((state) => state.hasAllPermissions(permissions))
}

/**
 * Get the user's current profile and permissions
 */
export function useAuth() {
  return useAuthStore((state) => ({
    profile: state.profile,
    permissions: state.profile?.permissions ?? [],
    isAuthenticated: state.isAuthenticated,
    isLoading: state.isLoading,
    accessToken: state.accessToken,
  }))
}

/**
 * Check if user is authenticated
 */
export function useIsAuthenticated(): boolean {
  return useAuthStore((state) => state.isAuthenticated)
}

/**
 * Check if authentication is loading
 */
export function useIsAuthLoading(): boolean {
  return useAuthStore((state) => state.isLoading)
}

/**
 * Get authentication error
 */
export function useAuthError(): string | null {
  return useAuthStore((state) => state.error)
}

/**
 * Check if token is expired
 */
export function useIsTokenExpired(): boolean {
  return useAuthStore((state) => state.isTokenExpired())
}

/**
 * Check if token is expiring soon (default 60 seconds)
 */
export function useIsTokenExpiringSoon(seconds?: number): boolean {
  return useAuthStore((state) => state.isTokenExpiringSoon(seconds))
}
