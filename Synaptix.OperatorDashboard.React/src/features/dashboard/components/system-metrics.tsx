/**
 * System-wide metrics display
 */

import type { SystemMetrics } from '../types'

interface SystemMetricsProps {
  metrics: SystemMetrics | undefined
  isLoading: boolean
}

export function SystemMetrics({ metrics, isLoading }: SystemMetricsProps) {
  if (isLoading || !metrics) {
    return (
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[...Array(4)].map((_, i) => (
          <div key={i} className="operator-card h-24 bg-bg-secondary animate-pulse" />
        ))}
      </div>
    )
  }

  const getResourceColor = (usage: number) => {
    if (usage < 50) return 'text-status-healthy'
    if (usage < 75) return 'text-status-degraded'
    return 'text-status-offline'
  }

  const getResourceBgColor = (usage: number) => {
    if (usage < 50) return 'bg-status-healthy/10'
    if (usage < 75) return 'bg-status-degraded/10'
    return 'bg-status-offline/10'
  }

  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
      {/* Requests */}
      <div className="operator-card">
        <p className="text-xs text-ink-tertiary">API Requests (1h)</p>
        <p className="text-2xl font-bold text-accent mt-2">
          {(metrics.apiGatewayRequests / 1000).toFixed(1)}k
        </p>
        <p className="text-xs text-ink-secondary mt-1">requests processed</p>
      </div>

      {/* Connections */}
      <div className="operator-card">
        <p className="text-xs text-ink-tertiary">Active Connections</p>
        <p className="text-2xl font-bold text-accent mt-2">{metrics.activeConnections}</p>
        <p className="text-xs text-ink-secondary mt-1">clients connected</p>
      </div>

      {/* CPU */}
      <div className="operator-card">
        <p className="text-xs text-ink-tertiary">CPU Usage</p>
        <div className="flex items-end gap-2 mt-2">
          <p className={`text-2xl font-bold ${getResourceColor(metrics.cpuUsage)}`}>
            {metrics.cpuUsage.toFixed(1)}%
          </p>
          <div className="flex-1 h-8 bg-bg-secondary rounded overflow-hidden">
            <div
              className={`h-full ${getResourceBgColor(metrics.cpuUsage)}`}
              style={{ width: `${metrics.cpuUsage}%` }}
            />
          </div>
        </div>
      </div>

      {/* Memory */}
      <div className="operator-card">
        <p className="text-xs text-ink-tertiary">Memory Usage</p>
        <div className="flex items-end gap-2 mt-2">
          <p className={`text-2xl font-bold ${getResourceColor(metrics.memoryUsage)}`}>
            {metrics.memoryUsage.toFixed(1)}%
          </p>
          <div className="flex-1 h-8 bg-bg-secondary rounded overflow-hidden">
            <div
              className={`h-full ${getResourceBgColor(metrics.memoryUsage)}`}
              style={{ width: `${metrics.memoryUsage}%` }}
            />
          </div>
        </div>
      </div>

      {/* Disk */}
      <div className="operator-card">
        <p className="text-xs text-ink-tertiary">Disk Usage</p>
        <div className="flex items-end gap-2 mt-2">
          <p className={`text-2xl font-bold ${getResourceColor(metrics.diskUsage)}`}>
            {metrics.diskUsage.toFixed(1)}%
          </p>
          <div className="flex-1 h-8 bg-bg-secondary rounded overflow-hidden">
            <div
              className={`h-full ${getResourceBgColor(metrics.diskUsage)}`}
              style={{ width: `${metrics.diskUsage}%` }}
            />
          </div>
        </div>
      </div>

      {/* Response Time */}
      <div className="operator-card">
        <p className="text-xs text-ink-tertiary">Avg Response Time</p>
        <p className="text-2xl font-bold text-accent mt-2">{metrics.avgResponseTime}ms</p>
        <p className="text-xs text-ink-secondary mt-1">latency</p>
      </div>

      {/* Error Rate */}
      <div className="operator-card">
        <p className="text-xs text-ink-tertiary">Error Rate</p>
        <p className={`text-2xl font-bold mt-2 ${getResourceColor(metrics.errorRate)}`}>
          {metrics.errorRate.toFixed(2)}%
        </p>
        <p className="text-xs text-ink-secondary mt-1">failed requests</p>
      </div>
    </div>
  )
}
