/**
 * Service health status card
 */

import type { ServiceHealth } from '../types'

interface ServiceCardProps {
  service: ServiceHealth
  sparklineData?: number[]
}

const STATUS_CONFIG = {
  healthy: {
    color: 'bg-status-healthy/10 border-status-healthy text-status-healthy',
    icon: '✓',
    label: 'Healthy',
  },
  degraded: {
    color: 'bg-status-degraded/10 border-status-degraded text-status-degraded',
    icon: '!',
    label: 'Degraded',
  },
  offline: {
    color: 'bg-status-offline/10 border-status-offline text-status-offline',
    icon: '✕',
    label: 'Offline',
  },
}

export function ServiceCard({ service, sparklineData }: ServiceCardProps) {
  const config = STATUS_CONFIG[service.status]

  return (
    <div className={`operator-card border-l-4 ${config.color}`}>
      <div className="flex items-start justify-between mb-3">
        <div>
          <h3 className="font-semibold text-ink-primary">{service.displayName}</h3>
          <p className="text-xs text-ink-tertiary mt-1">{service.description}</p>
        </div>
        <span className={`inline-flex items-center justify-center w-8 h-8 rounded font-bold text-sm ${config.color}`}>
          {config.icon}
        </span>
      </div>

      {/* Metrics Grid */}
      <div className="grid grid-cols-3 gap-2 mt-4 pt-4 border-t border-panel-border">
        <div>
          <p className="text-xs text-ink-tertiary">Uptime</p>
          <p className="text-lg font-bold text-ink-primary mt-1">{service.uptime.toFixed(2)}%</p>
        </div>
        <div>
          <p className="text-xs text-ink-tertiary">Response</p>
          <p className="text-lg font-bold text-ink-primary mt-1">{service.responseTime}ms</p>
        </div>
        <div>
          <p className="text-xs text-ink-tertiary">Status</p>
          <p className="text-sm font-medium mt-1">{config.label}</p>
        </div>
      </div>

      {/* Sparkline */}
      {sparklineData && sparklineData.length > 0 && (
        <div className="mt-4 pt-4 border-t border-panel-border">
          <p className="text-xs text-ink-tertiary mb-2">24h Trend</p>
          <svg className="w-full h-12" viewBox="0 0 100 40" preserveAspectRatio="none">
            <defs>
              <linearGradient id={`gradient-${service.id}`} x1="0%" y1="0%" x2="0%" y2="100%">
                <stop offset="0%" stopColor={service.status === 'healthy' ? '#10b981' : service.status === 'degraded' ? '#f59e0b' : '#ef4444'} stopOpacity="0.3" />
                <stop offset="100%" stopColor={service.status === 'healthy' ? '#10b981' : service.status === 'degraded' ? '#f59e0b' : '#ef4444'} stopOpacity="0" />
              </linearGradient>
            </defs>

            {/* Area under line */}
            <path
              d={`M 0 ${40 - (sparklineData[0] || 0) * 0.4} ${sparklineData
                .map((d, i) => `L ${(i / (sparklineData.length - 1)) * 100} ${40 - d * 0.4}`)
                .join(' ')} L 100 40 L 0 40 Z`}
              fill={`url(#gradient-${service.id})`}
            />

            {/* Line */}
            <polyline
              points={sparklineData.map((d, i) => `${(i / (sparklineData.length - 1)) * 100},${40 - d * 0.4}`).join(' ')}
              fill="none"
              stroke={service.status === 'healthy' ? '#10b981' : service.status === 'degraded' ? '#f59e0b' : '#ef4444'}
              strokeWidth="1.5"
            />
          </svg>
        </div>
      )}

      {/* Last Check */}
      <p className="text-xs text-ink-tertiary mt-4 pt-4 border-t border-panel-border">
        Last checked: {new Date(service.lastCheckedAt).toLocaleTimeString()}
      </p>
    </div>
  )
}
