import { getAccessToken, refresh } from './auth'
import { emit } from './adminAnalytics'
import { apiBase } from './config'

const API_BASE = apiBase()

export class ApiError extends Error {
  constructor(
    public status: number,
    public code: string,
    message: string,
    public endpoint?: string,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

function parseErrorBody(body: unknown): { code: string; message: string } {
  if (!body || typeof body !== 'object') return { code: 'UNKNOWN', message: '' }

  const obj = body as Record<string, unknown>
  const err = (obj.error ?? obj) as Record<string, unknown>

  return {
    code: typeof err.code === 'string' ? err.code : 'UNKNOWN',
    message: typeof err.message === 'string' ? err.message : '',
  }
}

async function throwApiError(res: Response, path: string, method?: string, startMs?: number): Promise<never> {
  let code = 'UNKNOWN'
  let message = res.statusText

  try {
    const body = await res.json()
    const parsed = parseErrorBody(body)

    code = parsed.code || code
    message = parsed.message || message
  }
  catch {
    // response wasn't JSON
  }

  const error = new ApiError(res.status, code, message, path)

  if (method && startMs) {
    emitEvent(method, path, res.status, code, startMs, false)
  }

  logApiError(error)
  throw error
}

function logApiError(error: ApiError) {
  const tag = error.code !== 'UNKNOWN' ? error.code : `HTTP_${error.status}`

  console.warn(`[admin-api] ${tag} ${error.endpoint ?? ''}`, {
    status: error.status,
    code: error.code,
    message: error.message,
  })
}

function emitEvent(method: string, path: string, status: number, errorCode: string | null, startMs: number, success: boolean) {
  emit({
    timestamp: Date.now(),
    endpoint: path,
    method,
    status,
    errorCode,
    latencyMs: Date.now() - startMs,
    success,
  })
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const startMs = Date.now()

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  }

  const token = getAccessToken()

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const adminOpsKey = import.meta.env.VITE_ADMIN_OPS_KEY as string | undefined

  if (adminOpsKey) {
    headers['X-Admin-Ops-Key'] = adminOpsKey
  }

  const res = await fetch(`${API_BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  })

  // 401 -> try refresh once, then retry
  if (res.status === 401 && token) {
    const refreshed = await refresh()

    if (refreshed) {
      headers.Authorization = `Bearer ${getAccessToken()}`

      const retry = await fetch(`${API_BASE}${path}`, {
        method,
        headers,
        body: body ? JSON.stringify(body) : undefined,
      })

      if (retry.ok) {
        emitEvent(method, path, retry.status, null, startMs, true)

        return retry.status === 204 ? (undefined as T) : retry.json()
      }

      return throwApiError(retry, path, method, startMs)
    }

    const error = new ApiError(401, 'UNAUTHORIZED', 'Session expired', path)

    emitEvent(method, path, 401, 'UNAUTHORIZED', startMs, false)
    logApiError(error)
    throw error
  }

  if (!res.ok) {
    return throwApiError(res, path, method, startMs)
  }

  emitEvent(method, path, res.status, null, startMs, true)

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
  delete: <T>(path: string) => request<T>('DELETE', path),
}
