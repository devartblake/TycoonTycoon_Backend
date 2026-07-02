/**
 * Dashboard Home page - System Health Overview
 */

import { useMemo } from 'react'
import { usePermission } from '@/hooks/use-permission'
import { ServiceCard } from '../components/service-card'
import { SystemMetrics } from '../components/system-metrics'
import { AlertsSection } from '../components/alerts-section'
import { useDashboardStats, useAllServiceHistory } from '../hooks/useDashboard'

export default function DashboardHomePage() {
  usePermission('operations:read')

  const statsQuery = useDashboardStats()
  const historyQuery = useAllServiceHistory(24)

  // Create a map of service histories for sparkline data
  const sparklineMap = useMemo(() => {
    if (!historyQuery.data) return {}
    return Object.fromEntries(
      historyQuery.data.map((h) => [h.serviceId, h.metrics.map((m) => m.value)])
    )
  }, [historyQuery.data])

  const stats = statsQuery.data
  const isLoading = statsQuery.isLoading || historyQuery.isLoading

  return (
    <div className="operator-container space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-ink-primary">Dashboard</h1>
        <p className="mt-2 text-ink-secondary">System health and performance overview</p>
      </div>

      {/* System Metrics */}
      <div>
        <h2 className="text-lg font-semibold text-ink-primary mb-4">System Metrics</h2>
        <SystemMetrics metrics={stats?.metrics} isLoading={isLoading} />
      </div>

      {/* Alerts */}
      {stats && (
        <AlertsSection alertCount={stats.alertsActive} services={stats.services} />
      )}

      {/* Services Grid */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-ink-primary">Service Status</h2>
          {stats && (
            <p className="text-sm text-ink-secondary">
              {stats.services.filter((s) => s.status === 'healthy').length} / {stats.services.length} operational
            </p>
          )}
        </div>

        {isLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {[...Array(6)].map((_, i) => (
              <div key={i} className="operator-card h-48 bg-bg-secondary animate-pulse" />
            ))}
          </div>
        ) : stats ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {stats.services.map((service) => (
              <ServiceCard
                key={service.id}
                service={service}
                sparklineData={sparklineMap[service.id]}
              />
            ))}
          </div>
        ) : null}
      </div>

      {/* Footer Info */}
      {stats && (
        <div className="text-xs text-ink-tertiary text-center pt-4 border-t border-panel-border">
          <p>Auto-refreshing every 30 seconds • Last updated: {new Date(stats.lastUpdatedAt).toLocaleTimeString()}</p>
          <p className="mt-1">Total health checks: {stats.checksPerformed.toLocaleString()}</p>
        </div>
      )}
    </div>
  )
}
