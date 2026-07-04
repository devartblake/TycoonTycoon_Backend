/**
 * Forgot password page
 */

import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import ErrorBoundary from '@/components/shared/error-boundary'
import { adminForgotPassword } from '@/features/auth/api'

const forgotPasswordSchema = z.object({
  email: z.string().email('Invalid email address'),
})

type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>

export default function ForgotPasswordPage() {
  const [isLoading, setIsLoading] = useState(false)
  const [isSubmitted, setIsSubmitted] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
    watch,
  } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
  })

  const email = watch('email')

  const onSubmit = async (data: ForgotPasswordFormData) => {
    setIsLoading(true)
    setError(null)

    try {
      await adminForgotPassword(data.email)
      setIsSubmitted(true)
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to send reset email.'
      setError(errorMessage)
    } finally {
      setIsLoading(false)
    }
  }

  if (isSubmitted) {
    return (
      <ErrorBoundary>
        <div className="space-y-4">
        <h2 className="text-2xl font-bold text-center text-ink-primary">Check your email</h2>
        <p className="text-center text-ink-secondary">
          We've sent a password reset link to <strong>{email}</strong>. Click the link in the email to reset your password.
        </p>
        <p className="text-center text-sm text-ink-tertiary">
          The link will expire in 15 minutes.
        </p>
        <div className="text-center pt-4">
          <Link to="/auth/login" className="text-sm text-accent hover:underline">
            Back to login
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

      <p className="text-sm text-ink-secondary text-center">
        Enter your email address and we'll send you a link to reset your password.
      </p>

      <div>
        <label htmlFor="email" className="block text-sm font-medium text-ink-primary mb-1">
          Email address
        </label>
        <input
          {...register('email')}
          id="email"
          type="email"
          className="w-full px-3 py-2 border border-panel-border rounded focus-ring"
          placeholder="admin@synaptix.com"
        />
        {errors.email && (
          <p className="text-xs text-status-offline mt-1">{errors.email.message}</p>
        )}
      </div>

      <button
        type="submit"
        disabled={isLoading}
        className="w-full px-4 py-2 bg-accent text-white rounded font-medium hover:bg-accent-dark transition-smooth disabled:opacity-50"
      >
        {isLoading ? 'Sending...' : 'Send reset link'}
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
