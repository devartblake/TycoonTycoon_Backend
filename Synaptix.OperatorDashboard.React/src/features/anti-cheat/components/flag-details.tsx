/**
 * Anti-cheat flag details viewer
 */

import { formatDate } from '@/lib/utils'
import type { AntiCheatFlag } from '../types'

interface FlagDetailsProps {
  flag: AntiCheatFlag | null
  isLoading: boolean
}

export function FlagDetails({ flag, isLoading }: FlagDetailsProps) {
  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="h-48 bg-bg-secondary rounded animate-pulse" />
      </div>
    )
  }

  if (!flag) {
    return (
      <div className="text-center py-12 text-ink-secondary">
        <p>No flag to review</p>
      </div>
    )
  }

  const severityColor = {
    low: 'bg-status-healthy/10 text-status-healthy',
    medium: 'bg-status-degraded/10 text-status-degraded',
    high: 'bg-status-offline/10 text-status-offline',
    critical: 'bg-red-500/10 text-red-600',
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="operator-card space-y-4">
        <div className="flex items-start justify-between">
          <div>
            <h2 className="text-2xl font-bold text-ink-primary">{flag.playerEmail}</h2>
            <p className="text-sm text-ink-secondary mt-1">Session {flag.sessionId}</p>
          </div>
          <span className={`inline-block px-3 py-1 rounded text-sm font-medium capitalize ${severityColor[flag.flagSeverity]}`}>
            {flag.flagSeverity}
          </span>
        </div>

        <div className="grid grid-cols-2 gap-4 pt-4 border-t border-panel-border">
          <div>
            <p className="text-xs text-ink-tertiary">Session Time</p>
            <p className="text-sm text-ink-primary mt-1">{formatDate(flag.sessionTime)}</p>
          </div>
          <div>
            <p className="text-xs text-ink-tertiary">Flag Reason</p>
            <p className="text-sm text-ink-primary mt-1">{flag.flagReason}</p>
          </div>
        </div>
      </div>

      {/* Telemetry */}
      <div className="operator-card space-y-4">
        <h3 className="font-semibold text-ink-primary">Telemetry Analysis</h3>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-bg-secondary rounded p-3">
            <p className="text-xs text-ink-tertiary">Avg Response Time</p>
            <p className="text-lg font-bold text-ink-primary mt-1">{Math.round(flag.telemetryData.avgResponseTime)}ms</p>
          </div>
          <div className="bg-bg-secondary rounded p-3">
            <p className="text-xs text-ink-tertiary">Response Variance</p>
            <p className="text-lg font-bold text-ink-primary mt-1">±{Math.round(flag.telemetryData.responseTimeVariance)}ms</p>
          </div>
          <div className="bg-bg-secondary rounded p-3">
            <p className="text-xs text-ink-tertiary">Accuracy Rate</p>
            <p className="text-lg font-bold text-status-healthy mt-1">{Math.round(flag.telemetryData.accuracyRate)}%</p>
          </div>
        </div>

        {/* Suspicious Patterns */}
        <div className="pt-4 border-t border-panel-border">
          <p className="text-sm font-medium text-ink-primary mb-2">Suspicious Patterns</p>
          <ul className="space-y-1">
            {flag.telemetryData.suspiciousPatterns.map((pattern, i) => (
              <li key={i} className="text-sm text-ink-secondary flex gap-2">
                <span className="text-status-offline">⚠</span>
                <span>{pattern}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  )
}
