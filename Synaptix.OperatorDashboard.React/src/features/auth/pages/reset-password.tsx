/**
 * Reset password page
 */

import { useState, useEffect } from 'react'
import { useSearchParams, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import ErrorBoundary from '@/components/shared/error-boundary'
import { adminResetPassword, adminValidateResetToken } from '@/features/auth/api'

const resetPasswordSchema = z
  .object({
    newPassword: z.string().min(8, 'Password must be at least 8 characters'),
    confirmPassword: z.string(),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  })

type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token')

  const [isLoading, setIsLoading] = useState(false)
  const [isValidating, setIsValidating] = useState(true)
  const [isSubmitted, setIsSubmitted] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [tokenError, setTokenError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
  })

  // Validate token on mount
  useEffect(() => {
    const validateToken = async () => {
      if (!token) {
        setTokenError('Reset token is missing. Please use the link from your email.')
        setIsValidating(false)
        return
      }

      try {
        await adminValidateResetToken(token)
        setIsValidating(false)
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Invalid or expired reset token.'
        setTokenError(errorMessage)
        setIsValidating(false)
      }
    }

    validateToken()
  }, [token])

  const onSubmit = async (data: ResetPasswordFormData) => {
    if (!token) return

    setIsLoading(true)
    setError(null)

    try {
      await adminResetPassword(token, data.newPassword, data.confirmPassword)
      setIsSubmitted(true)
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to reset password.'
      setError(errorMessage)
    } finally {
      setIsLoading(false)
    }
  }

  if (isValidating) {
    return (
      <ErrorBoundary>
        <div className="space-y-4 text-center">
          <h2 className="text-2xl font-bold text-ink-primary">Validating reset link...</h2>
        </div>
      </ErrorBoundary>
    )
  }

  if (tokenError) {
    return (
      <ErrorBoundary>
        <div className="space-y-4">
          <div className="p-4 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm">
            {tokenError}
          </div>
          <div className="text-center">
            <Link to="/auth/forgot-password" className="text-sm text-accent hover:underline">
              Request a new reset link
            </Link>
          </div>
        </div>
      </ErrorBoundary>
    )
  }

  if (isSubmitted) {
    return (
      <ErrorBoundary>
        <div className="space-y-4">
          <h2 className="text-2xl font-bold text-center text-ink-primary">Password reset successful</h2>
          <p className="text-center text-ink-secondary">
            Your password has been reset. You can now log in with your new password.
          </p>
          <div className="text-center pt-4">
            <Link to="/auth/login" className="text-sm text-accent hover:underline">
              Go to login
            </Link>
          </div>
        </div>
      </ErrorBoundary>
    )
  }

  return (
    <ErrorBoundary>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <h2 className="text-2xl font-bold text-center text-ink-primary mb-6">Reset your password</h2>

      {error && (
        <div className="p-4 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm">
          {error}
        </div>
      )}

      <div>
        <label htmlFor="newPassword" className="block text-sm font-medium text-ink-primary mb-1">
          New password
        </label>
        <input
          {...register('newPassword')}
          id="newPassword"
          type="password"
          className="w-full px-3 py-2 border border-panel-border rounded focus-ring"
          placeholder="••••••••"
        />
        {errors.newPassword && (
          <p className="text-xs text-status-offline mt-1">{errors.newPassword.message}</p>
        )}
      </div>

      <div>
        <label htmlFor="confirmPassword" className="block text-sm font-medium text-ink-primary mb-1">
          Confirm password
        </label>
        <input
          {...register('confirmPassword')}
          id="confirmPassword"
          type="password"
          className="w-full px-3 py-2 border border-panel-border rounded focus-ring"
          placeholder="••••••••"
        />
        {errors.confirmPassword && (
          <p className="text-xs text-status-offline mt-1">{errors.confirmPassword.message}</p>
        )}
      </div>

      <button
        type="submit"
        disabled={isLoading}
        className="w-full px-4 py-2 bg-accent text-white rounded font-medium hover:bg-accent-dark transition-smooth disabled:opacity-50"
      >
        {isLoading ? 'Resetting...' : 'Reset password'}
      </button>

      <div className="text-center">
        <Link to="/auth/login" className="text-sm text-accent hover:underline">
          Back to login
        </Link>
      </div>
      </form>
    </ErrorBoundary>
  )
}
