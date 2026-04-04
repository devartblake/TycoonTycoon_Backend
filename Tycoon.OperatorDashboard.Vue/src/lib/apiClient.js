function normalizeError(payload, fallbackMessage) {
  const maybeMessage = payload?.error?.message ?? payload?.message
  return {
    code: payload?.error?.code ?? 'UNKNOWN_ERROR',
    message: maybeMessage || fallbackMessage
  }
}

export async function apiGetJson(path) {
  const response = await fetch(path, { credentials: 'include' })
  return await parseJsonResponse(response)
}

export async function apiPostJson(path, body = undefined) {
  const response = await fetch(path, {
    method: 'POST',
    credentials: 'include',
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body)
  })
  return await parseJsonResponse(response)
}

async function parseJsonResponse(response) {
  const contentType = response.headers.get('content-type') || ''
  const isJson = contentType.includes('application/json')
  const payload = isJson ? await response.json() : null

  if (!response.ok) {
    throw normalizeError(payload, `Request failed (${response.status})`)
  }

  return payload
}
