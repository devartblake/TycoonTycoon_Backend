import { ApiError } from './apiClient'
import { logout } from './auth'

// ─── Error codes matching backend security envelope ─────────────────

export type ApiErrorCode = 'UNAUTHORIZED' | 'FORBIDDEN' | 'RATE_LIMITED' | 'VALIDATION_ERROR' | 'NOT_FOUND' | 'CONFLICT'

/** Type guard: is this an ApiError thrown by apiClient? */
export function isApiError(err: unknown): err is ApiError {
  return err instanceof ApiError
}

/** Extract the error code from a caught error, or null if not an ApiError. */
export function getErrorCode(err: unknown): ApiErrorCode | null {
  if (!isApiError(err)) return null

  const known: ApiErrorCode[] = ['UNAUTHORIZED', 'FORBIDDEN', 'RATE_LIMITED', 'VALIDATION_ERROR', 'NOT_FOUND', 'CONFLICT']

  return known.includes(err.code as ApiErrorCode) ? (err.code as ApiErrorCode) : null
}

/** Get the validation field details from a VALIDATION_ERROR, if present. */
export function getValidationDetails(err: unknown): Record<string, string[]> | null {
  if (!isApiError(err) || err.code !== 'VALIDATION_ERROR') return null

  // The backend puts field-level errors under error.details
  try {
    const parsed = JSON.parse(err.message)

    if (parsed.details && typeof parsed.details === 'object') {
      return parsed.details
    }
  } catch {
    // message is not JSON — no field-level detail
  }

  return null
}

// ─── Centralized error handler ──────────────────────────────────────

export interface ErrorHandlerResult {
  message: string
  severity: 'error' | 'warning' | 'info'
  shouldLogout: boolean
  retryAfterMs: number | null
  code: ApiErrorCode | null
  fieldErrors: Record<string, string[]> | null
}

/**
 * Central error handler for all admin API errors.
 *
 * Maps backend `error.code` values to actionable UX behavior per the
 * security error envelope contract (docs/security_error_envelope_contract.md).
 *
 * Usage:
 *   catch (err) {
 *     const result = handleApiError(err)
 *     setError(result)
 *     if (result.shouldLogout) logout()
 *   }
 */
export function handleApiError(err: unknown): ErrorHandlerResult {
  if (!isApiError(err)) {
    // Network errors, timeouts, etc.
    const message = err instanceof Error ? err.message : 'An unexpected error occurred'

    return {
      message,
      severity: 'error',
      shouldLogout: false,
      retryAfterMs: null,
      code: null,
      fieldErrors: null
    }
  }

  const code = getErrorCode(err)

  switch (code) {
    case 'UNAUTHORIZED':
      // Session expired or invalid — redirect to login
      return {
        message: 'Your session has expired. Please log in again.',
        severity: 'warning',
        shouldLogout: true,
        retryAfterMs: null,
        code,
        fieldErrors: null
      }

    case 'FORBIDDEN':
      // Authenticated but lacking permissions
      return {
        message: err.message || 'You do not have permission to perform this action.',
        severity: 'error',
        shouldLogout: false,
        retryAfterMs: null,
        code,
        fieldErrors: null
      }

    case 'RATE_LIMITED':
      // Throttled — disable repeated actions
      return {
        message: 'Too many requests. Please wait a moment before trying again.',
        severity: 'warning',
        shouldLogout: false,
        retryAfterMs: 15_000,
        code,
        fieldErrors: null
      }

    case 'VALIDATION_ERROR':
      return {
        message: err.message || 'Please check the form for errors.',
        severity: 'warning',
        shouldLogout: false,
        retryAfterMs: null,
        code,
        fieldErrors: getValidationDetails(err)
      }

    case 'NOT_FOUND':
      return {
        message: err.message || 'The requested resource was not found.',
        severity: 'info',
        shouldLogout: false,
        retryAfterMs: null,
        code,
        fieldErrors: null
      }

    case 'CONFLICT':
      return {
        message: err.message || 'This action conflicts with the current state. Please refresh and try again.',
        severity: 'warning',
        shouldLogout: false,
        retryAfterMs: null,
        code,
        fieldErrors: null
      }

    default:
      // Unrecognized error code — fall back to generic message
      return {
        message: err.message || `Request failed (${err.status})`,
        severity: 'error',
        shouldLogout: false,
        retryAfterMs: null,
        code: null,
        fieldErrors: null
      }
  }
}

/**
 * Handle the side effects of an API error result (logout redirect).
 * Call this after updating component state with the error.
 */
export function applyErrorSideEffects(result: ErrorHandlerResult) {
  if (result.shouldLogout) {
    logout()
  }
}
