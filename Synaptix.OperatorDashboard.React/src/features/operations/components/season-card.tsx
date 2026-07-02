/**
 * Season status card
 */

import { Button } from '@/components/ui/button'
import type { Season } from '../types'

interface SeasonCardProps {
  season: Season
  onAction: (action: 'start' | 'close') => Promise<void>
  isLoading: boolean
}

const STATUS_CONFIG = {
  draft: { color: 'text-ink-secondary', bg: 'bg-ink-secondary/10', label: 'Draft', icon: '📝' },
  scheduled: { color: 'text-ink-tertiary', bg: 'bg-ink-tertiary/10', label: 'Scheduled', icon: '📅' },
  active: { color: 'text-status-healthy', bg: 'bg-status-healthy/10', label: 'Active', icon: '🔴' },
  ended: { color: 'text-status-offline', bg: 'bg-status-offline/10', label: 'Ended', icon: '⏹' },
}

export function SeasonCard({ season, onAction, isLoading }: SeasonCardProps) {
  const config = STATUS_CONFIG[season.status]
  const now = new Date()
  const start = new Date(season.startDate)
  const end = new Date(season.endDate)
  const daysRemaining = Math.ceil((end.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))

  return (
    <div className={`operator-card border-l-4 ${config.bg}`}>
      <div className="flex items-start justify-between gap-4 mb-4">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="text-lg">{config.icon}</span>
            <h3 className="text-lg font-bold text-ink-primary">{season.name}</h3>
          </div>
          <p className="text-sm text-ink-secondary">{season.description}</p>
        </div>
        <span className={`px-3 py-1 rounded text-xs font-medium whitespace-nowrap ${config.bg} ${config.color}`}>
          {config.label}
        </span>
      </div>

      {/* Timeline */}
      <div className="pt-3 pb-3 border-t border-b border-panel-border">
        <div className="flex items-center gap-2 text-xs text-ink-tertiary mb-2">
          <span>Start: {start.toLocaleDateString()}</span>
          <span>→</span>
          <span>End: {end.toLocaleDateString()}</span>
        </div>
        {season.status === 'active' && daysRemaining > 0 && (
          <p className="text-xs text-status-healthy font-medium">{daysRemaining} days remaining</p>
        )}
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-3 gap-3 mt-4 mb-4">
        <div>
          <p className="text-xs text-ink-tertiary">Reward Pool</p>
          <p className="text-sm font-bold text-accent mt-1">
            {(season.rewardPool / 1000).toFixed(0)}k
          </p>
        </div>
        <div>
          <p className="text-xs text-ink-tertiary">Points Multiplier</p>
          <p className="text-sm font-bold text-accent mt-1">
            {season.pointsMultiplier.toFixed(1)}x
          </p>
        </div>
        <div>
          <p className="text-xs text-ink-tertiary">Season</p>
          <p className="text-sm font-bold text-accent mt-1"># {season.number}</p>
        </div>
      </div>

      {/* Actions */}
      <div className="flex gap-2 pt-4 border-t border-panel-border">
        {season.status === 'scheduled' && (
          <Button
            onClick={() => onAction('start')}
            disabled={isLoading}
            size="sm"
            className="flex-1 text-xs"
          >
            {isLoading ? 'Starting...' : 'Start Season'}
          </Button>
        )}
        {season.status === 'active' && (
          <Button
            onClick={() => onAction('close')}
            disabled={isLoading}
            variant="outline"
            size="sm"
            className="flex-1 text-xs"
          >
            {isLoading ? 'Closing...' : 'End Season'}
          </Button>
        )}
        {(season.status === 'draft' || season.status === 'ended') && (
          <div className="flex-1 text-xs text-ink-tertiary">
            {season.status === 'ended' ? 'Season ended' : 'Awaiting schedule'}
          </div>
        )}
      </div>
    </div>
  )
}
