import { getAccessToken, refresh } from './auth'
import { apiBase } from './config'

const API_BASE = apiBase()

export class ApiError extends Error {
  constructor(
    public status: number,
    public code: string,
    message: string
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json'
  }

  const token = getAccessToken()

  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  const adminOpsKey = process.env.NEXT_PUBLIC_ADMIN_OPS_KEY

  if (adminOpsKey) {
    headers['X-Admin-Ops-Key'] = adminOpsKey
  }

  const res = await fetch(`${API_BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined
  })

  // 401 → try refresh once, then retry
  if (res.status === 401 && token) {
    const refreshed = await refresh()

    if (refreshed) {
      headers['Authorization'] = `Bearer ${getAccessToken()}`

      const retry = await fetch(`${API_BASE}${path}`, {
        method,
        headers,
        body: body ? JSON.stringify(body) : undefined
      })

      if (retry.ok) {
        return retry.status === 204 ? (undefined as T) : retry.json()
      }
    }

    // Refresh failed — caller should redirect to login
    throw new ApiError(401, 'UNAUTHORIZED', 'Session expired')
  }

  if (!res.ok) {
    let code = 'UNKNOWN'
    let message = res.statusText

    try {
      const body = await res.json()

      // Backend may return { error: { code, message, details } } or flat { code, message }
      const err = body.error ?? body

      code = err.code ?? code
      message = err.message ?? message
    } catch {
      // response wasn't JSON
    }

    throw new ApiError(res.status, code, message)
  }

  if (res.status === 204) {
    return undefined as T
  }

  return res.json()
}

export const apiClient = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body),
  patch: <T>(path: string, body?: unknown) => request<T>('PATCH', path, body),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body),
  delete: <T>(path: string) => request<T>('DELETE', path)
}
