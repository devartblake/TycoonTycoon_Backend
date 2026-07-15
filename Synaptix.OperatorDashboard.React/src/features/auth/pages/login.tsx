/**
 * Login page
 */

import { startTransition, useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import ErrorBoundary from '@/components/shared/error-boundary'
import { persistAuthState, useAuthStore } from '@/features/auth/store'
import { adminLogin } from '@/features/auth/api'
import { getMockMode, setMockMode } from '@/lib/api-config'

const loginSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(6, 'Password must be at least 6 characters'),
})

type LoginFormData = z.infer<typeof loginSchema>

export default function LoginPage() {
  const navigate = useNavigate()
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const setTokens = useAuthStore((state) => state.setTokens)
  const setProfile = useAuthStore((state) => state.setProfile)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  })

  const goToDashboard = () => {
    // Lazy AppLayout/Dashboard suspend on first load. Navigating during a form
    // submit is "synchronous input"; without startTransition React 18 throws #426
    // and the UI blanks. Transition marks this as non-urgent so Suspense can show.
    startTransition(() => {
      navigate('/dashboard', { replace: true })
    })
  }

  const onSubmit = async (data: LoginFormData) => {
    setIsLoading(true)
    setError(null)

    try {
      // Handle mock mode - allow any email/password
      if (getMockMode()) {
        setTokens('mock-token-' + Date.now(), 'mock-refresh-' + Date.now(), 3600)
        setProfile({
          email: data.email,
          permissions: [
            'users:read',
            'users:write',
            'notifications:read',
            'notifications:write',
            'anti-cheat:read',
            'anti-cheat:write',
            'audit:read',
            'events:read',
            'storage:read',
            'config:read',
            'economy:read',
            'content:read',
            'operations:read',
            'personalization:read',
          ],
        })
        persistAuthState()
        goToDashboard()
        return
      }

      // Call login endpoint
      const response = await adminLogin(data.email, data.password)

      // Store tokens and profile
      setTokens(response.accessToken, response.refreshToken, response.expiresIn)
      setProfile(response.admin)
      persistAuthState()

      goToDashboard()
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Login failed. Please try again.'
      setError(errorMessage)
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <ErrorBoundary>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <h2 className="text-2xl font-bold text-center text-ink-primary mb-6">Sign in to your account</h2>

      {error && (
        <div className="p-4 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm">
          {error}
        </div>
      )}

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

      <div>
        <label htmlFor="password" className="block text-sm font-medium text-ink-primary mb-1">
          Password
        </label>
        <input
          {...register('password')}
          id="password"
          type="password"
          className="w-full px-3 py-2 border border-panel-border rounded focus-ring"
          placeholder="••••••••"
        />
        {errors.password && (
          <p className="text-xs text-status-offline mt-1">{errors.password.message}</p>
        )}
      </div>

      <button
        type="submit"
        disabled={isLoading}
        className="w-full px-4 py-2 bg-accent text-white rounded font-medium hover:bg-accent-dark transition-smooth disabled:opacity-50"
      >
        {isLoading ? 'Signing in...' : 'Sign in'}
      </button>

      {!getMockMode() && (
        <div className="p-3 bg-blue-50 border border-blue-200 rounded text-sm text-blue-900">
          <p className="mb-2">
            🎭 <strong>No backend?</strong> Enable mock mode to test the UI with simulated data.
          </p>
          <button
            type="button"
            onClick={() => {
              setMockMode(true)
              window.location.reload()
            }}
            className="text-blue-700 hover:underline font-medium text-xs"
          >
            Enable Mock Mode
          </button>
        </div>
      )}

      {getMockMode() && (
        <div className="p-3 bg-yellow-50 border border-yellow-200 rounded text-sm text-yellow-900">
          <p>🎭 <strong>Mock mode enabled</strong> — Enter any email and password to proceed</p>
        </div>
      )}

      <div className="text-center">
        <Link to="/auth/forgot-password" className="text-sm text-accent hover:underline">
          Forgot your password?
        </Link>
      </div>
      </form>
    </ErrorBoundary>
  )
}
