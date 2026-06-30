/**
 * Main app layout with sidebar and top navigation
 */

import React from 'react'
import { Outlet } from 'react-router-dom'
import { useIsAuthenticated } from '@/hooks/use-permission'
import { Sidebar } from './sidebar'
import { TopNav } from './top-nav'
import { MockBanner } from '@/components/shared/mock-banner'

export default function AppLayout() {
  const isAuthenticated = useIsAuthenticated()

  if (!isAuthenticated) {
    // Redirect to login if not authenticated
    window.location.href = '/auth/login'
    return null
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
