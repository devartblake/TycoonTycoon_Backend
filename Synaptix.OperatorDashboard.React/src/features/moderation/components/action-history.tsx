/**
 * Moderation action history timeline
 */

import { formatDateTime } from '@/lib/utils'
import type { ModerationAction } from '../types'

interface ActionHistoryProps {
  actions: ModerationAction[]
  isLoading: boolean
}

const ACTION_CONFIG = {
  ban: { icon: '🚫', color: 'text-status-offline', bg: 'bg-status-offline/10' },
  unban: { icon: '✓', color: 'text-status-healthy', bg: 'bg-status-healthy/10' },
  suspend: { icon: '⏸', color: 'text-status-degraded', bg: 'bg-status-degraded/10' },
  unsuspend: { icon: '▶', color: 'text-status-healthy', bg: 'bg-status-healthy/10' },
  warn: { icon: '⚠', color: 'text-status-degraded', bg: 'bg-status-degraded/10' },
  note: { icon: '📝', color: 'text-ink-secondary', bg: 'bg-ink-secondary/10' },
}

export function ActionHistory({ actions, isLoading }: ActionHistoryProps) {
  if (isLoading) {
    return (
      <div className="operator-card space-y-2">
        {[...Array(4)].map((_, i) => (
          <div key={i} className="h-12 bg-bg-secondary rounded animate-pulse" />
        ))}
      </div>
    )
  }

  if (actions.length === 0) {
    return (
      <div className="text-center py-8 text-ink-secondary operator-card">
        <p>No moderation actions</p>
      </div>
    )
  }

  return (
    <div className="operator-card">
      <h3 className="font-semibold text-ink-primary mb-4">Moderation History</h3>

      <div className="space-y-3">
        {actions.map((action) => {
          const config = ACTION_CONFIG[action.action]

          return (
            <div key={action.id} className={`p-3 rounded border border-panel-border ${config.bg}`}>
              <div className="flex items-start gap-3">
                <span className="text-lg">{config.icon}</span>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <p className={`font-semibold text-sm capitalize ${config.color}`}>
                      {action.action === 'note' ? 'Moderator Note' : action.action}
                    </p>
                    {action.status !== 'active' && (
                      <span className="text-xs text-ink-tertiary">({action.status})</span>
                    )}
                  </div>
                  <p className="text-sm text-ink-secondary mt-1">{action.reason}</p>
                  {action.notes && (
                    <p className="text-xs text-ink-tertiary mt-1 italic">Notes: {action.notes}</p>
                  )}
                  {action.duration && (
                    <p className="text-xs text-ink-tertiary mt-1">
                      Duration: {Math.round(action.duration / (60 * 60 * 1000))}h
                      {action.expiresAt && ` (expires: ${new Date(action.expiresAt).toLocaleDateString()})`}
                    </p>
                  )}
                  <div className="flex items-center justify-between gap-2 mt-2">
                    <p className="text-xs text-ink-tertiary">
                      by {action.adminEmail}
                    </p>
                    <p className="text-xs text-ink-tertiary">
                      {formatDateTime(action.createdAt)}
                    </p>
                  </div>
                </div>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
