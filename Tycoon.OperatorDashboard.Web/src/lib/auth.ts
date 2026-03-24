import type { AdminLoginResponse, AdminProfile, AdminRefreshResponse } from './types/admin'
import { apiBase } from './config'

const API_BASE = apiBase()

// ─── In-memory token store (client-side only) ────────────────────────

let accessToken: string | null = null
let tokenExpiresAt = 0 // epoch ms
let currentAdmin: AdminProfile | null = null

function persistRefreshToken(token: string) {
  try {
    localStorage.setItem('refreshToken', token)
  } catch {
    // SSR or storage unavailable
  }
}

function loadRefreshToken(): string | null {
  try {
    return localStorage.getItem('refreshToken')
  } catch {
    return null
  }
}

function clearTokens() {
  accessToken = null
  tokenExpiresAt = 0
  currentAdmin = null

  try {
    localStorage.removeItem('refreshToken')
  } catch {
    // SSR
  }
}

// ─── Public API ──────────────────────────────────────────────────────

export async function login(email: string, password: string): Promise<AdminProfile> {
  const res = await fetch(`${API_BASE}/admin/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  })

  if (!res.ok) {
    let message = 'Login failed'

    try {
      const err = await res.json()

      message = err.message ?? message
    } catch {
      // non-JSON response
    }

    throw new Error(message)
  }

  const data: AdminLoginResponse = await res.json()

  accessToken = data.accessToken
  tokenExpiresAt = Date.now() + data.expiresIn * 1000
  currentAdmin = data.admin
  persistRefreshToken(data.refreshToken)

  return data.admin
}

export async function refresh(): Promise<boolean> {
  const refreshToken = loadRefreshToken()

  if (!refreshToken) return false

  try {
    const res = await fetch(`${API_BASE}/admin/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken })
    })

    if (!res.ok) {
      clearTokens()

      return false
    }

    const data: AdminRefreshResponse = await res.json()

    accessToken = data.accessToken
    tokenExpiresAt = Date.now() + data.expiresIn * 1000

    return true
  } catch {
    clearTokens()

    return false
  }
}

export function getAccessToken(): string | null {
  // Proactively refresh if expiring within 60s
  if (accessToken && tokenExpiresAt - Date.now() < 60_000) {
    // Fire refresh in background — current token is still valid
    refresh()
  }

  return accessToken
}

export function getAdmin(): AdminProfile | null {
  return currentAdmin
}

export async function fetchProfile(): Promise<AdminProfile | null> {
  const token = getAccessToken()

  if (!token) return null

  try {
    const res = await fetch(`${API_BASE}/admin/auth/me`, {
      headers: { Authorization: `Bearer ${token}` }
    })

    if (!res.ok) return null

    const profile: AdminProfile = await res.json()

    currentAdmin = profile

    return profile
  } catch {
    return null
  }
}

export function logout() {
  clearTokens()

  // Redirect to login (only in browser)
  if (typeof window !== 'undefined') {
    window.location.href = '/login'
  }
}

export function isAuthenticated(): boolean {
  return !!accessToken || !!loadRefreshToken()
}
