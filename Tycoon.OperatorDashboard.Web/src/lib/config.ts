/**
 * Returns the API base URL.
 *
 * - If NEXT_PUBLIC_API_URL is set (production/Docker), use it directly.
 * - Otherwise, use the Next.js proxy prefix so requests are rewritten
 *   by next.config.mjs to the backend (avoids CORS in dev).
 */
export function apiBase(): string {
  const explicit = process.env.NEXT_PUBLIC_API_URL

  if (explicit) return explicit

  // In dev, all API calls go through the Next.js proxy rewrite:
  //   /api/backend/admin/auth/login  →  http://localhost:5000/admin/auth/login
  return '/api/backend'
}
