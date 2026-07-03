/**
 * Seasons & Game Events - Lifecycle Management
 */

import { useState } from 'react'
import { usePermission } from '@/hooks/use-permission'
import { SeasonCard } from '../components/season-card'
import { EventCard } from '../components/event-card'
import {
  useSeasons,
  useGameEvents,
  useOperationsStats,
  usePerformLifecycleAction,
} from '../hooks/useOperations'
import type { SeasonFilter, EventFilter } from '../types'

export default function LifecyclePage() {
  usePermission('operations:write')

  const [seasonFilters, setSeasonFilters] = useState<SeasonFilter>({})
  const [eventFilters, setEventFilters] = useState<EventFilter>({})
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const seasonsQuery = useSeasons(seasonFilters)
  const eventsQuery = useGameEvents(eventFilters)
  const statsQuery = useOperationsStats()
  const actionMutation = usePerformLifecycleAction()

  const seasons = seasonsQuery.data?.items || []
  const events = eventsQuery.data?.items || []

  const handleSeasonAction = async (seasonId: string, action: 'start' | 'close') => {
    try {
      await actionMutation.mutateAsync({
        resourceId: seasonId,
        resourceType: 'season',
        action,
        notes: `${action === 'start' ? 'Started' : 'Closed'} via operator dashboard`,
      })
      const message = action === 'start' ? 'Season started' : 'Season ended'
      setSuccessMessage(message)
      setTimeout(() => setSuccessMessage(null), 2000)
    } catch (err) {
      setSuccessMessage(err instanceof Error ? err.message : 'Action failed')
    }
  }

  const handleEventAction = async (eventId: string, action: 'start' | 'close' | 'cancel') => {
    try {
      await actionMutation.mutateAsync({
        resourceId: eventId,
        resourceType: 'event',
        action,
        notes: `Event ${action} via operator dashboard`,
      })
      const messages = { start: 'Event opened', close: 'Event closed', cancel: 'Event cancelled' }
      setSuccessMessage(messages[action])
      setTimeout(() => setSuccessMessage(null), 2000)
    } catch (err) {
      setSuccessMessage(err instanceof Error ? err.message : 'Action failed')
    }
  }

  return (
    <div className="operator-container space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-ink-primary">Seasons & Game Events</h1>
        <p className="mt-2 text-ink-secondary">Manage lifecycle and monitor progress</p>
      </div>

      {/* Stats */}
      {statsQuery.data && (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Active Seasons</p>
            <p className="text-2xl font-bold text-accent mt-1">
              {statsQuery.data.activeSeasons}
            </p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Upcoming Events</p>
            <p className="text-2xl font-bold text-accent mt-1">
              {statsQuery.data.upcomingEvents}
            </p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Total Participants</p>
            <p className="text-2xl font-bold text-status-healthy mt-1">
              {(statsQuery.data.totalParticipants / 1000).toFixed(0)}k
            </p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Reward Pool</p>
            <p className="text-2xl font-bold text-accent mt-1">
              {(statsQuery.data.rewardPoolRemaining / 1000000).toFixed(1)}M
            </p>
          </div>
        </div>
      )}

      {/* Success Message */}
      {successMessage && (
        <div className="p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm">
          ✓ {successMessage}
        </div>
      )}

      {/* Seasons Section */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-semibold text-ink-primary">Seasons</h2>
          <div className="flex gap-2">
            {['draft', 'scheduled', 'active', 'ended'].map((status) => (
              <button
                key={status}
                onClick={() => setSeasonFilters({ status: seasonFilters.status === status ? undefined : (status as any) })}
                className={`px-3 py-1 text-xs rounded border transition-colors ${
                  seasonFilters.status === status
                    ? 'bg-accent text-white border-accent'
                    : 'border-panel-border hover:bg-bg-secondary text-ink-secondary'
                }`}
              >
                {status}
              </button>
            ))}
          </div>
        </div>

        {seasonsQuery.isLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="operator-card h-64 bg-bg-secondary animate-pulse" />
            ))}
          </div>
        ) : seasons.length > 0 ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {seasons.map((season) => (
              <SeasonCard
                key={season.id}
                season={season}
                onAction={(action) => handleSeasonAction(season.id, action)}
                isLoading={actionMutation.isPending}
              />
            ))}
          </div>
        ) : (
          <div className="text-center py-8 text-ink-secondary operator-card">
            <p>No seasons found</p>
          </div>
        )}
      </div>

      {/* Events Section */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-semibold text-ink-primary">Game Events</h2>
          <div className="flex gap-2 overflow-x-auto">
            {['draft', 'upcoming', 'active', 'ended', 'cancelled'].map((status) => (
              <button
                key={status}
                onClick={() => setEventFilters({ status: eventFilters.status === status ? undefined : (status as any) })}
                className={`px-3 py-1 text-xs rounded border transition-colors whitespace-nowrap ${
                  eventFilters.status === status
                    ? 'bg-accent text-white border-accent'
                    : 'border-panel-border hover:bg-bg-secondary text-ink-secondary'
                }`}
              >
                {status}
              </button>
            ))}
          </div>
        </div>

        {eventsQuery.isLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="operator-card h-64 bg-bg-secondary animate-pulse" />
            ))}
          </div>
        ) : events.length > 0 ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {events.map((event) => (
              <EventCard
                key={event.id}
                event={event}
                onAction={(action) => handleEventAction(event.id, action)}
                isLoading={actionMutation.isPending}
              />
            ))}
          </div>
        ) : (
          <div className="text-center py-8 text-ink-secondary operator-card">
            <p>No events found</p>
          </div>
        )}
      </div>
    </div>
  )
}
