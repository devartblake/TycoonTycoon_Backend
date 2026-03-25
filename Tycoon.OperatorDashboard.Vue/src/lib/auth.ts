import type { AdminLoginResponse, AdminProfile, AdminRefreshResponse } from './types/admin'
import { ApiError } from './apiClient'
import { apiBase } from './config'

const API_BASE = apiBase()

let accessToken: string | null = null
let tokenExpiresAt = 0
let currentAdmin: AdminProfile | null = null

function persistRefreshToken(token: string) {
  try {
    localStorage.setItem('refreshToken', token)
  }
  catch {
    // storage unavailable
  }
}

function loadRefreshToken(): string | null {
  try {
    return localStorage.getItem('refreshToken')
  }
  catch {
    return null
  }
}

function clearTokens() {
  accessToken = null
  tokenExpiresAt = 0
  currentAdmin = null

  try {
    localStorage.removeItem('refreshToken')
  }
  catch {
    // storage unavailable
  }
}

export async function login(email: string, password: string): Promise<AdminProfile> {
  const res = await fetch(`${API_BASE}/admin/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })

  if (!res.ok) {
    let code = 'UNKNOWN'
    let message = 'Login failed'

    try {
      const body = await res.json()
      const err = body.error ?? body

      code = err.code ?? code
      message = err.message ?? message
    }
    catch {
      // non-JSON response
    }

    throw new ApiError(res.status, code, message)
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
      body: JSON.stringify({ refreshToken }),
    })

    if (!res.ok) {
      clearTokens()

      return false
    }

    const data: AdminRefreshResponse = await res.json()

    accessToken = data.accessToken
    tokenExpiresAt = Date.now() + data.expiresIn * 1000

    return true
  }
  catch {
    clearTokens()

    return false
  }
}

export function getAccessToken(): string | null {
  if (accessToken && tokenExpiresAt - Date.now() < 60_000) {
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

  const res = await fetch(`${API_BASE}/admin/auth/me`, {
    headers: { Authorization: `Bearer ${token}` },
  })

  if (!res.ok) {
    if (res.status === 403) {
      let code = 'FORBIDDEN'
      let message = 'You do not have permission to access the admin dashboard.'

      try {
        const body = await res.json()
        const err = (body as Record<string, unknown>).error ?? body

        code = (err as Record<string, unknown>).code as string ?? code
        message = (err as Record<string, unknown>).message as string ?? message
      }
      catch {
        // non-JSON response
      }

      throw new ApiError(403, code, message)
    }

    return null
  }

  const profile: AdminProfile = await res.json()

  currentAdmin = profile

  return profile
}

export function logout() {
  clearTokens()

  if (typeof window !== 'undefined') {
    window.location.href = '/login'
  }
}

export function isAuthenticated(): boolean {
  return !!accessToken || !!loadRefreshToken()
}
