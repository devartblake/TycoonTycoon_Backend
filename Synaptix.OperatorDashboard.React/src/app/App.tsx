/**
 * Main App component
 * Providers and routing setup
 */

import { useEffect } from 'react'
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
      <RouterProvider router={router} />
      <Toaster />
    </QueryClientProvider>
  )
}

export default AppContent
