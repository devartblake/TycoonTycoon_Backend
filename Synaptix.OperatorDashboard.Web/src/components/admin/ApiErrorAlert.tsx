'use client'

import { useEffect, useState } from 'react'

import Alert from '@mui/material/Alert'
import AlertTitle from '@mui/material/AlertTitle'
import Collapse from '@mui/material/Collapse'

import type { ErrorHandlerResult } from '@/lib/apiErrors'

interface ApiErrorAlertProps {
  error: ErrorHandlerResult | null
  onClose?: () => void
}

/**
 * Renders an API error as a contextual alert matching the error code.
 *
 * - UNAUTHORIZED: warning with session-expired message
 * - FORBIDDEN: error with permission-denied message
 * - RATE_LIMITED: warning that auto-dismisses after the retry cooldown
 * - VALIDATION_ERROR: warning with field-level detail
 * - NOT_FOUND: info with stale-resource hint
 * - CONFLICT: warning with state-conflict hint
 */
const ApiErrorAlert = ({ error, onClose }: ApiErrorAlertProps) => {
  const [visible, setVisible] = useState(!!error)

  useEffect(() => {
    setVisible(!!error)
  }, [error])

  // Auto-dismiss rate-limit alerts after the cooldown period
  useEffect(() => {
    if (!error?.retryAfterMs) return

    const timer = setTimeout(() => {
      setVisible(false)
      onClose?.()
    }, error.retryAfterMs)

    return () => clearTimeout(timer)
  }, [error, onClose])

  if (!error || !visible) return null

  const titleMap: Record<string, string> = {
    UNAUTHORIZED: 'Session Expired',
    FORBIDDEN: 'Permission Denied',
    RATE_LIMITED: 'Rate Limited',
    VALIDATION_ERROR: 'Validation Error',
    NOT_FOUND: 'Not Found',
    CONFLICT: 'Conflict'
  }

  const title = error.code ? titleMap[error.code] : undefined

  return (
    <Collapse in={visible}>
      <Alert
        severity={error.severity}
        onClose={() => {
          setVisible(false)
          onClose?.()
        }}
        sx={{ mb: 2 }}
      >
        {title && <AlertTitle>{title}</AlertTitle>}
        {error.message}
        {error.fieldErrors && (
          <ul style={{ margin: '8px 0 0', paddingLeft: 20 }}>
            {Object.entries(error.fieldErrors).map(([field, messages]) =>
              messages.map((msg, i) => (
                <li key={`${field}-${i}`}>
                  <strong>{field}:</strong> {msg}
                </li>
              ))
            )}
          </ul>
        )}
      </Alert>
    </Collapse>
  )
}

export default ApiErrorAlert
