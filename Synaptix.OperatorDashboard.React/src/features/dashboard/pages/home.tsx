/**
 * Dashboard home page
 */

// import React from 'react'
import { useAuth } from '@/hooks/use-permission'

export default function DashboardHome() {
  const { profile } = useAuth()

  return (
    <div className="operator-container">
      <div className="space-y-8">
        <div>
          <h1 className="text-3xl font-bold text-ink-primary">Welcome, {profile?.email}!</h1>
          <p className="mt-2 text-ink-secondary">Dashboard coming soon...</p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="operator-card">
            <div className="text-sm text-ink-tertiary">Users</div>
            <div className="text-2xl font-bold text-accent mt-2">—</div>
          </div>
          <div className="operator-card">
            <div className="text-sm text-ink-tertiary">Flagged Sessions</div>
            <div className="text-2xl font-bold text-accent mt-2">—</div>
          </div>
          <div className="operator-card">
            <div className="text-sm text-ink-tertiary">Moderation Queue</div>
            <div className="text-2xl font-bold text-accent mt-2">—</div>
          </div>
          <div className="operator-card">
            <div className="text-sm text-ink-tertiary">System Status</div>
            <div className="text-2xl font-bold text-status-healthy mt-2">Healthy</div>
          </div>
        </div>

        <div className="bg-bg-secondary p-6 rounded border border-panel-border">
          <h2 className="text-lg font-semibold text-ink-primary mb-4">Quick Links</h2>
          <ul className="space-y-2 text-sm text-ink-secondary">
            <li>• Users and moderation tools</li>
            <li>• Security and audit logs</li>
            <li>• Store and economy management</li>
            <li>• Content and notifications</li>
            <li>• Configuration and diagnostics</li>
          </ul>
        </div>
      </div>
    </div>
  )
}
