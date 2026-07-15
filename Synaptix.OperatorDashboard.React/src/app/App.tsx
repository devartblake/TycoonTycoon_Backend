/**
 * Main App component
 * Providers and routing setup
 */

import { Suspense, useEffect } from 'react'
import { RouterProvider } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { Toaster } from '@/components/ui/toaster'
import { router } from './router'
import { queryClient } from '@/lib/query-client'
import { useAuthStore, restoreAuthState } from '@/features/auth/store'
import '@/index.css'

function AppContent() {
  const setLoading = useAuthStore((state) => state.setLoading)

  // Restore auth state from storage on app init
  useEffect(() => {
    setLoading(true)
    restoreAuthState().finally(() => {
      setLoading(false)
    })
  }, [setLoading])

  return (
    <QueryClientProvider client={queryClient}>
      {/* React.lazy route modules need a Suspense ancestor above RouterProvider */}
      <Suspense
        fallback={
          <div className="min-h-screen flex items-center justify-center text-ink-secondary">
            Loading…
          </div>
        }
      >
        <RouterProvider router={router} />
      </Suspense>
      <Toaster />
    </QueryClientProvider>
  )
}

export default AppContent
