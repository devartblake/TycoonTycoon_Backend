/**
 * Zustand auth store with RBAC system
 * Manages authentication state, tokens, profile, and permissions
 */

import { create } from 'zustand'
import type { AdminProfile, AuthState, Permission } from '@/types/auth'

interface AuthStore extends AuthState {
  // Auth actions
  setTokens: (accessToken: string, refreshToken: string, expiresIn: number) => void
  setProfile: (profile: AdminProfile) => void
  setError: (error: string | null) => void
  setLoading: (loading: boolean) => void
  logout: () => void
  reset: () => void

  // Permission checks
  hasPermission: (permission: Permission) => boolean
  hasAnyPermission: (permissions: Permission[]) => boolean
  hasAllPermissions: (permissions: Permission[]) => boolean

  // Token management
  isTokenExpired: () => boolean
  isTokenExpiringSoon: (seconds?: number) => boolean
  updateTokenExpiry: (expiresIn: number) => void
}

const INITIAL_STATE: AuthState = {
  accessToken: null,
  refreshToken: null,
  profile: null,
  // true until restoreAuthState finishes so AppLayout does not redirect to login on first paint
  isLoading: true,
  error: null,
  isAuthenticated: false,
  expiresAt: null,
}

const TOKEN_REFRESH_BUFFER = 60 // Refresh token 60 seconds before expiry

export const useAuthStore = create<AuthStore>((set, get) => ({
  ...INITIAL_STATE,

  setTokens: (accessToken, refreshToken, expiresIn) => {
    const expiresAt = Date.now() + expiresIn * 1000
    set({
      accessToken,
      refreshToken,
      expiresAt,
      isAuthenticated: true,
      error: null,
    })
  },

  setProfile: (profile) => {
    set({
      profile,
      isAuthenticated: true,
    })
  },

  setError: (error) => {
    set({ error })
  },

  setLoading: (loading) => {
    set({ isLoading: loading })
  },

  logout: () => {
    // Clear tokens from secure storage (httpOnly cookies handled by backend)
    try {
      localStorage.removeItem('auth_state')
    } catch {
      // ignore storage errors in private mode
    }
    set({
      ...INITIAL_STATE,
      isAuthenticated: false,
    })
  },

  reset: () => {
    set(INITIAL_STATE)
  },

  // RBAC: Check if user has a specific permission
  hasPermission: (permission: Permission) => {
    const { profile } = get()
    if (!profile) return false
    return profile.permissions.includes(permission)
  },

  // RBAC: Check if user has ANY of the specified permissions
  hasAnyPermission: (permissions: Permission[]) => {
    const { profile } = get()
    if (!profile) return false
    return permissions.some((p) => profile.permissions.includes(p))
  },

  // RBAC: Check if user has ALL of the specified permissions
  hasAllPermissions: (permissions: Permission[]) => {
    const { profile } = get()
    if (!profile) return false
    return permissions.every((p) => profile.permissions.includes(p))
  },

  // Token expiry checks
  isTokenExpired: () => {
    const { expiresAt } = get()
    if (!expiresAt) return true
    return Date.now() >= expiresAt
  },

  isTokenExpiringSoon: (seconds = TOKEN_REFRESH_BUFFER) => {
    const { expiresAt } = get()
    if (!expiresAt) return true
    return Date.now() >= expiresAt - seconds * 1000
  },

  updateTokenExpiry: (expiresIn: number) => {
    const expiresAt = Date.now() + expiresIn * 1000
    set({ expiresAt })
  },
}))

/**
 * Restore auth state from localStorage (called on app init)
 * In production, tokens should be stored in httpOnly cookies instead
 */
export const restoreAuthState = async () => {
  try {
    const stored = localStorage.getItem('auth_state')
    if (stored) {
      const state = JSON.parse(stored)
      // Only restore if tokens exist and haven't expired
      if (state.accessToken && state.expiresAt && Date.now() < state.expiresAt) {
        useAuthStore.setState({
          accessToken: state.accessToken,
          refreshToken: state.refreshToken,
          profile: state.profile,
          expiresAt: state.expiresAt,
          isAuthenticated: true,
        })
      } else {
        // Clear expired state
        localStorage.removeItem('auth_state')
      }
    }
  } catch (error) {
    console.error('Failed to restore auth state:', error)
    localStorage.removeItem('auth_state')
  }
}

/**
 * Persist auth state to localStorage (called after login)
 * In production, use httpOnly cookies for secure token storage
 */
export const persistAuthState = () => {
  const state = useAuthStore.getState()
  if (state.accessToken && state.profile) {
    localStorage.setItem(
      'auth_state',
      JSON.stringify({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        profile: state.profile,
        expiresAt: state.expiresAt,
      })
    )
  }
}
