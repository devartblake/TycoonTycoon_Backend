/**
 * Game event status card
 */

import { Button } from '@/components/ui/button'
import type { GameEvent } from '../types'

interface EventCardProps {
  event: GameEvent
  onAction: (action: 'start' | 'close' | 'cancel') => Promise<void>
  isLoading: boolean
}

const STATUS_CONFIG = {
  draft: { color: 'text-ink-secondary', bg: 'bg-ink-secondary/10', label: 'Draft', icon: '📝' },
  upcoming: { color: 'text-ink-tertiary', bg: 'bg-ink-tertiary/10', label: 'Upcoming', icon: '⏳' },
  active: { color: 'text-status-healthy', bg: 'bg-status-healthy/10', label: 'Active', icon: '🔴' },
  ended: { color: 'text-status-offline', bg: 'bg-status-offline/10', label: 'Ended', icon: '⏹' },
  cancelled: { color: 'text-status-offline', bg: 'bg-status-offline/10', label: 'Cancelled', icon: '✕' },
}

const TYPE_ICON = {
  tournament: '🏆',
  challenge: '⚔️',
  promotion: '🎉',
  special: '✨',
}

export function EventCard({ event, onAction, isLoading }: EventCardProps) {
  const config = STATUS_CONFIG[event.status]
  const participation = (event.participantCount / event.maxParticipants) * 100

  return (
    <div className={`operator-card border-l-4 ${config.bg}`}>
      <div className="flex items-start justify-between gap-4 mb-4">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="text-lg">{TYPE_ICON[event.type]}</span>
            <h3 className="text-lg font-bold text-ink-primary">{event.name}</h3>
          </div>
          <p className="text-sm text-ink-secondary">{event.description}</p>
        </div>
        <span className={`px-3 py-1 rounded text-xs font-medium whitespace-nowrap ${config.bg} ${config.color}`}>
          {config.label}
        </span>
      </div>

      {/* Timeline */}
      <div className="pt-3 pb-3 border-t border-b border-panel-border">
        <div className="flex items-center gap-2 text-xs text-ink-tertiary">
          <span>{new Date(event.startDate).toLocaleDateString()}</span>
          <span>→</span>
          <span>{new Date(event.endDate).toLocaleDateString()}</span>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-3 gap-3 mt-4 mb-4">
        <div>
          <p className="text-xs text-ink-tertiary">Reward</p>
          <p className="text-sm font-bold text-accent mt-1">
            {(event.reward / 1000).toFixed(0)}k
          </p>
        </div>
        <div>
          <p className="text-xs text-ink-tertiary">Participants</p>
          <p className="text-sm font-bold text-accent mt-1">
            {event.participantCount.toLocaleString()}
          </p>
        </div>
        <div>
          <p className="text-xs text-ink-tertiary">Capacity</p>
          <p className="text-sm font-bold text-accent mt-1">
            {Math.round(participation)}%
          </p>
        </div>
      </div>

      {/* Participation Bar */}
      <div className="mb-4">
        <div className="w-full h-2 bg-bg-secondary rounded overflow-hidden">
          <div
            className={`h-full transition-all ${
              participation > 80
                ? 'bg-status-offline'
                : participation > 50
                  ? 'bg-status-degraded'
                  : 'bg-status-healthy'
            }`}
            style={{ width: `${Math.min(participation, 100)}%` }}
          />
        </div>
      </div>

      {/* Actions */}
      <div className="flex gap-2 pt-4 border-t border-panel-border">
        {event.status === 'upcoming' && (
          <Button
            onClick={() => onAction('start')}
            disabled={isLoading}
            size="sm"
            className="flex-1 text-xs"
          >
            {isLoading ? 'Opening...' : 'Open Event'}
          </Button>
        )}
        {event.status === 'active' && (
          <>
            <Button
              onClick={() => onAction('close')}
              disabled={isLoading}
              variant="outline"
              size="sm"
              className="flex-1 text-xs"
            >
              {isLoading ? 'Closing...' : 'Close'}
            </Button>
            <Button
              onClick={() => onAction('cancel')}
              disabled={isLoading}
              variant="outline"
              size="sm"
              className="flex-1 text-xs text-status-offline"
            >
              Cancel
            </Button>
          </>
        )}
        {(event.status === 'draft' || event.status === 'ended' || event.status === 'cancelled') && (
          <div className="flex-1 text-xs text-ink-tertiary text-center">
            {event.status === 'ended' ? 'Event concluded' : 'Awaiting schedule'}
          </div>
        )}
      </div>
    </div>
  )
}
