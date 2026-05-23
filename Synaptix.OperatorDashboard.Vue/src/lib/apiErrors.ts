import { ApiError } from './apiClient'
import { logout } from './auth'

export type ApiErrorCode = 'UNAUTHORIZED' | 'FORBIDDEN' | 'RATE_LIMITED' | 'VALIDATION_ERROR' | 'NOT_FOUND' | 'CONFLICT'

export function isApiError(err: unknown): err is ApiError {
  return err instanceof ApiError
}

export function getErrorCode(err: unknown): ApiErrorCode | null {
  if (!isApiError(err)) return null

  const known: ApiErrorCode[] = ['UNAUTHORIZED', 'FORBIDDEN', 'RATE_LIMITED', 'VALIDATION_ERROR', 'NOT_FOUND', 'CONFLICT']

  return known.includes(err.code as ApiErrorCode) ? (err.code as ApiErrorCode) : null
}

export function getValidationDetails(err: unknown): Record<string, string[]> | null {
  if (!isApiError(err) || err.code !== 'VALIDATION_ERROR') return null

  try {
    const parsed = JSON.parse(err.message)

    if (parsed.details && typeof parsed.details === 'object') {
      return parsed.details
    }
  }
  catch {
    // message is not JSON
  }

  return null
}

export interface ErrorHandlerResult {
  message: string
  severity: 'error' | 'warning' | 'info'
  shouldLogout: boolean
  retryAfterMs: number | null
  code: ApiErrorCode | null
  fieldErrors: Record<string, string[]> | null
}

export function handleApiError(err: unknown): ErrorHandlerResult {
  if (!isApiError(err)) {
    const message = err instanceof Error ? err.message : 'An unexpected error occurred'

    return {
      message,
      severity: 'error',
      shouldLogout: false,
      retryAfterMs: null,
      code: null,
      fieldErrors: null,
    }
  }

  const code = getErrorCode(err)

  switch (code) {
    case 'UNAUTHORIZED':
      return {
        message: 'Your session has expired. Please log in again.',
        severity: 'warning',
        shouldLogout: true,
        retryAfterMs: null,
        code,
        fieldErrors: null,
      }

    case 'FORBIDDEN':
      return {
        message: err.message || 'You do not have permission to perform this action.',
        severity: 'error',
        shouldLogout: false,
        retryAfterMs: null,
        code,
        fieldErrors: null,
      }

    case 'RATE_LIMITED':
      return {
        message: 'Too many requests. Please wait a moment before trying again.',
        severity: 'warning',
        shouldLogout: false,
        retryAfterMs: 15_000,
        code,
        fieldErrors: null,
      }

    case 'VALIDATION_ERROR':
      return {
        message: err.message || 'Please check the form for errors.',
        severity: 'warning',
        shouldLogout: false,
        retryAfterMs: null,
        code,
        fieldErrors: getValidationDetails(err),
      }

    case 'NOT_FOUND':
      return {
        message: err.message || 'The requested resource was not found.',
        severity: 'info',
        shouldLogout: false,
        retryAfterMs: null,
        code,
        fieldErrors: null,
      }

    case 'CONFLICT':
      return {
        message: err.message || 'This action conflicts with the current state. Please refresh and try again.',
        severity: 'warning',
        shouldLogout: false,
        retryAfterMs: null,
        code,
        fieldErrors: null,
      }

    default:
      return {
        message: err.message || `Request failed (${err.status})`,
        severity: 'error',
        shouldLogout: false,
        retryAfterMs: null,
        code: null,
        fieldErrors: null,
      }
  }
}

export function applyErrorSideEffects(result: ErrorHandlerResult) {
  if (result.shouldLogout) {
    logout()
  }
}
