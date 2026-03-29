'use client'

import { useCallback, useRef, useState } from 'react'

import type { ErrorHandlerResult } from '../apiErrors'
import { applyErrorSideEffects, handleApiError } from '../apiErrors'

/**
 * Hook for standardized API error handling in admin views.
 *
 * Returns:
 *   - error: current ErrorHandlerResult or null
 *   - handleError: pass a caught error to process it
 *   - clearError: dismiss the error
 *   - isRateLimited: true while a rate-limit cooldown is active
 */
export function useApiError() {
  const [error, setError] = useState<ErrorHandlerResult | null>(null)
  const [isRateLimited, setIsRateLimited] = useState(false)
  const rateLimitTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const clearError = useCallback(() => {
    setError(null)
  }, [])

  const handleError = useCallback((err: unknown) => {
    const result = handleApiError(err)

    setError(result)

    // Apply side effects (logout on UNAUTHORIZED)
    applyErrorSideEffects(result)

    // Track rate-limit cooldown
    if (result.retryAfterMs) {
      setIsRateLimited(true)

      if (rateLimitTimer.current) {
        clearTimeout(rateLimitTimer.current)
      }

      rateLimitTimer.current = setTimeout(() => {
        setIsRateLimited(false)
        rateLimitTimer.current = null
      }, result.retryAfterMs)
    }
  }, [])

  return { error, handleError, clearError, isRateLimited }
}
