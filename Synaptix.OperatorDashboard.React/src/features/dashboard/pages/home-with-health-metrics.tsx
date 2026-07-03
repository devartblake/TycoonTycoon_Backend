/**
 * Dashboard Home page - System Health Overview (with real health metrics)
 *
 * This is an enhanced version that uses real health check data from /health endpoint
 * alongside the mock dashboard stats for gradual migration.
 */

import { useMemo } from 'react'
import { usePermission } from '@/hooks/use-permission'
import { useHealthMetrics } from '@/hooks/use-health-metrics'
import { ServiceCard } from '../components/service-card'
import { SystemMetrics } from '../components/system-metrics'
import { AlertsSection } from '../components/alerts-section'
import { useDashboardStats, useAllServiceHistory } from '../hooks/useDashboard'

export default function DashboardHomePageWithHealth() {
  usePermission('operations:read')

  // Existing dashboard stats (can be phased out)
  const statsQuery = useDashboardStats()
  const historyQuery = useAllServiceHistory(24)

  // New: Real health metrics from backend
  const { metrics: healthMetrics, isLoading: healthLoading, error: healthError } = useHealthMetrics({
    enabled: true,
    pollInterval: 30000,
    onError: (error) => {
      console.warn('Health metrics unavailable, using fallback:', error)
    },
  })

  // Create a map of service histories for sparkline data
  const sparklineMap = useMemo(() => {
    if (!historyQuery.data) return {}
    return Object.fromEntries(
      historyQuery.data.map((h) => [h.serviceId, h.metrics.map((m) => m.value)])
    )
  }, [historyQuery.data])

  const stats = statsQuery.data
  const isLoading = statsQuery.isLoading || historyQuery.isLoading

  // Merge health metrics with stats for display
  // Prefer real health metrics over mock data
  const displayMetrics = healthMetrics || stats?.metrics

  return (
    <div className="operator-container space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-ink-primary">Dashboard</h1>
        <p className="mt-2 text-ink-secondary">System health and performance overview</p>

        {/* Health Status Indicator */}
        {healthMetrics && (
          <div className="mt-4 flex items-center gap-2">
            <div className={`w-3 h-3 rounded-full ${healthMetrics.isHealthy ? 'bg-status-healthy' : 'bg-status-offline'}`} />
            <span className={`text-sm font-medium ${healthMetrics.isHealthy ? 'text-status-healthy' : 'text-status-offline'}`}>
              {healthMetrics.isHealthy ? 'All Systems Operational' : 'System Issues Detected'}
            </span>
            {healthError && (
              <span className="text-xs text-status-offline ml-4">
                (Fallback mode: real metrics unavailable)
              </span>
            )}
          </div>
        )}
      </div>

      {/* System Metrics - Now with real data */}
      <div>
        <h2 className="text-lg font-semibold text-ink-primary mb-4">System Metrics</h2>
        <SystemMetrics
          metrics={displayMetrics}
          isLoading={isLoading || healthLoading}
        />
      </div>

      {/* Health Details - Real time endpoint checks */}
      {healthMetrics && (
        <div className="operator-card space-y-3">
          <h3 className="font-semibold text-ink-primary">Health Check Details</h3>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {/* Response Time */}
            <div className="space-y-1">
              <p className="text-xs text-ink-tertiary">Response Time (P95)</p>
              <p className="text-lg font-semibold text-accent">
                {healthMetrics.responseTime.toFixed(0)}ms
              </p>
              <p className={`text-xs ${healthMetrics.responseTime < 300 ? 'text-status-healthy' : healthMetrics.responseTime < 500 ? 'text-status-degraded' : 'text-status-offline'}`}>
                {healthMetrics.responseTime < 300 ? '✓ Excellent' : healthMetrics.responseTime < 500 ? '⚠ Elevated' : '✗ Degraded'}
              </p>
            </div>

            {/* Error Rate */}
            <div className="space-y-1">
              <p className="text-xs text-ink-tertiary">Error Rate</p>
              <p className="text-lg font-semibold text-accent">
                {(healthMetrics.errorRate * 100).toFixed(2)}%
              </p>
              <p className={`text-xs ${healthMetrics.errorRate < 0.001 ? 'text-status-healthy' : healthMetrics.errorRate < 0.01 ? 'text-status-degraded' : 'text-status-offline'}`}>
                {healthMetrics.errorRate < 0.001 ? '✓ Healthy' : healthMetrics.errorRate < 0.01 ? '⚠ Elevated' : '✗ High'}
              </p>
            </div>

            {/* Uptime */}
            <div className="space-y-1">
              <p className="text-xs text-ink-tertiary">Uptime</p>
              <p className="text-lg font-semibold text-accent">
                {formatUptime(healthMetrics.uptime)}
              </p>
              <p className="text-xs text-status-healthy">✓ Running</p>
            </div>
          </div>
        </div>
      )}

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
        <div className="text-xs text-ink-tertiary text-center pt-4 border-t border-panel-border space-y-1">
          <p>Auto-refreshing every 30 seconds • Last updated: {new Date(stats.lastUpdatedAt).toLocaleTimeString()}</p>
          <p>Total health checks: {stats.checksPerformed.toLocaleString()}</p>
          {healthMetrics && (
            <p className="text-ink-secondary">
              Real-time metrics from /health endpoint • {healthMetrics.isHealthy ? 'Healthy' : 'Issues Detected'}
            </p>
          )}
        </div>
      )}
    </div>
  )
}

/**
 * Format uptime duration
 */
function formatUptime(seconds: number): string {
  if (seconds < 60) return `${Math.floor(seconds)}s`
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m`
  if (seconds < 86400) return `${Math.floor(seconds / 3600)}h`
  return `${Math.floor(seconds / 86400)}d`
}
