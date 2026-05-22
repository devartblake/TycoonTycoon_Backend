let sessionCache = null

export async function getOperatorSession() {
  if (sessionCache) return sessionCache

  try {
    const response = await fetch('/api/me', { credentials: 'include' })
    if (!response.ok) throw new Error(`session fetch failed (${response.status})`)

    const data = await response.json()
    sessionCache = {
      authenticated: Boolean(data?.authenticated),
      name: data?.name ?? 'anonymous',
      permissions: Array.isArray(data?.permissions) ? data.permissions : []
    }

    return sessionCache
  } catch {
    sessionCache = { authenticated: false, name: 'anonymous', permissions: [] }
    return sessionCache
  }
}

export function clearSessionCache() {
  sessionCache = null
}
