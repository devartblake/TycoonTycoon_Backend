/**
 * Main app layout with sidebar and top navigation
 */

import React from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { useIsAuthenticated, useIsAuthLoading } from '@/hooks/use-permission'
import { Sidebar } from './sidebar'
import { TopNav } from './top-nav'
import { MockBanner } from '@/components/shared/mock-banner'

export default function AppLayout() {
  const isAuthenticated = useIsAuthenticated()
  const isAuthLoading = useIsAuthLoading()

  // Wait for restoreAuthState before bouncing to login (avoids flash/blank after mock login reload)
  if (isAuthLoading) {
    return <div className="min-h-screen flex items-center justify-center text-ink-secondary">Loading…</div>
  }

  if (!isAuthenticated) {
    return <Navigate to="/auth/login" replace />
  }

  return (
    <>
      <MockBanner />
      <div className="operator-shell pt-12">
        <Sidebar />
        <div className="operator-main flex flex-col flex-1">
          <TopNav />
          <main className="flex-1 overflow-auto">
            <React.Suspense fallback={<div className="p-8">Loading...</div>}>
              <Outlet />
            </React.Suspense>
          </main>
        </div>
      </div>
    </>
  )
}
