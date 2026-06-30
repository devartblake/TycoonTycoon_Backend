/**
 * Authentication layout (login, forgot password, reset password)
 */

import React from 'react'
import { Outlet } from 'react-router-dom'

export default function AuthLayout() {
  return (
    <div className="min-h-screen bg-bg-primary flex items-center justify-center py-12 px-4">
      <div className="w-full max-w-md space-y-8">
        <div className="text-center">
          <h1 className="text-3xl font-bold text-ink-primary">Synaptix</h1>
          <p className="mt-2 text-sm text-ink-tertiary">Operator Dashboard</p>
        </div>

        <div className="operator-card">
          <React.Suspense fallback={<div className="text-center py-8">Loading...</div>}>
            <Outlet />
          </React.Suspense>
        </div>

        <div className="text-center text-xs text-ink-tertiary">
          <p>&copy; 2026 Synaptix. All rights reserved.</p>
        </div>
      </div>
    </div>
  )
}
