/**
 * Anti-cheat queue statistics display
 */

import type { QueueStats } from '../types'

interface QueueStatsProps {
  stats: QueueStats
  isLoading: boolean
}

export function QueueStats({ stats, isLoading }: QueueStatsProps) {
  if (isLoading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {[...Array(3)].map((_, i) => (
          <div key={i} className="operator-card h-24 bg-bg-secondary animate-pulse" />
        ))}
      </div>
    )
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      <div className="operator-card">
        <div className="text-sm text-ink-tertiary">Pending Review</div>
        <div className="text-3xl font-bold text-accent mt-2">{stats.pendingCount}</div>
        <p className="text-xs text-ink-secondary mt-2">Flags awaiting verdict</p>
      </div>

      <div className="operator-card">
        <div className="text-sm text-ink-tertiary">Reviewed This Week</div>
        <div className="text-3xl font-bold text-status-healthy mt-2">{stats.reviewedThisWeek}</div>
        <p className="text-xs text-ink-secondary mt-2">Verdicts submitted</p>
      </div>

      <div className="operator-card">
        <div className="text-sm text-ink-tertiary">Completion Rate</div>
        <div className="text-3xl font-bold text-accent mt-2">{Math.round(stats.completionRate * 100)}%</div>
        <p className="text-xs text-ink-secondary mt-2">Of queue cleared weekly</p>
      </div>
    </div>
  )
}
