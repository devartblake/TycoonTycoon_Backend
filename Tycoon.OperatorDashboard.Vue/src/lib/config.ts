/**
 * Returns the API base URL.
 *
 * - If VITE_API_URL is set (production/Docker), use it directly.
 * - Otherwise, use the Vite dev proxy prefix so requests are rewritten
 *   by vite.config.ts to the backend (avoids CORS in dev).
 */
export function apiBase(): string {
  const explicit = import.meta.env.VITE_API_URL as string | undefined

  if (explicit) return explicit

  return '/api/backend'
}
