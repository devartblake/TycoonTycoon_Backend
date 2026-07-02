/**
 * Player activity timeline
 */

import { formatDateTime } from '@/lib/utils'
import type { PlayerActivity } from '../types'

interface ActivityTimelineProps {
  activities: PlayerActivity[]
  isLoading: boolean
}

const ACTIVITY_CONFIG = {
  login: { icon: '📱', color: 'text-ink-secondary', bg: 'bg-ink-secondary/10' },
  game_played: { icon: '🎮', color: 'text-accent', bg: 'bg-accent/10' },
  purchase: { icon: '🛒', color: 'text-status-healthy', bg: 'bg-status-healthy/10' },
  violation: { icon: '⚠️', color: 'text-status-offline', bg: 'bg-status-offline/10' },
  appeal: { icon: '📤', color: 'text-ink-tertiary', bg: 'bg-ink-tertiary/10' },
  action: { icon: '⚙️', color: 'text-status-degraded', bg: 'bg-status-degraded/10' },
}

export function ActivityTimeline({ activities, isLoading }: ActivityTimelineProps) {
  if (isLoading) {
    return (
      <div className="operator-card space-y-2">
        {[...Array(5)].map((_, i) => (
          <div key={i} className="h-12 bg-bg-secondary rounded animate-pulse" />
        ))}
      </div>
    )
  }

  if (activities.length === 0) {
    return (
      <div className="text-center py-8 text-ink-secondary operator-card">
        <p>No recent activity</p>
      </div>
    )
  }

  return (
    <div className="operator-card">
      <h3 className="font-semibold text-ink-primary mb-4">Activity Timeline</h3>

      <div className="space-y-3">
        {activities.map((activity) => {
          const config = ACTIVITY_CONFIG[activity.type]

          return (
            <div key={activity.id} className={`p-3 rounded border border-panel-border ${config.bg}`}>
              <div className="flex items-start gap-3">
                <span className="text-lg">{config.icon}</span>
                <div className="flex-1 min-w-0">
                  <p className={`font-semibold text-sm capitalize ${config.color}`}>
                    {activity.type.replace('_', ' ')}
                  </p>
                  <p className="text-sm text-ink-secondary mt-1">{activity.description}</p>
                  {activity.metadata && (
                    <div className="text-xs text-ink-tertiary mt-1 space-y-1">
                      {Object.entries(activity.metadata).map(([key, value]) => (
                        <p key={key}>
                          {key}: {typeof value === 'number' ? value.toLocaleString() : String(value)}
                        </p>
                      ))}
                    </div>
                  )}
                  <p className="text-xs text-ink-tertiary mt-2">
                    {formatDateTime(activity.timestamp)}
                  </p>
                </div>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
