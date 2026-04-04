function normalizeError(payload, fallbackMessage) {
  const maybeMessage = payload?.error?.message ?? payload?.message
  return {
    code: payload?.error?.code ?? 'UNKNOWN_ERROR',
    message: maybeMessage || fallbackMessage
  }
}

export async function apiGetJson(path) {
  const response = await fetch(path, { credentials: 'include' })
  const contentType = response.headers.get('content-type') || ''
  const isJson = contentType.includes('application/json')
  const payload = isJson ? await response.json() : null

  if (!response.ok) {
    throw normalizeError(payload, `Request failed (${response.status})`)
  }

  return payload
}
