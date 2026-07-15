/**
 * Seasons & Game Events lifecycle
 * - /operations/seasons → seasons only
 * - /operations/game-events → events only
 * - /operations → overview of both
 */

import { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonGrid } from '@/components/shared/skeletons'
import { SeasonCard } from '../components/season-card'
import { EventCard } from '../components/event-card'
import {
  useSeasons,
  useGameEvents,
  useOperationsStats,
  usePerformLifecycleAction,
} from '../hooks/useOperations'
import type { SeasonFilter, EventFilter } from '../types'

type OpsMode = 'overview' | 'seasons' | 'events'

function modeFromPath(pathname: string): OpsMode {
  if (pathname.includes('game-events')) return 'events'
  if (pathname.includes('seasons')) return 'seasons'
  return 'overview'
}

export default function LifecyclePage() {
  usePermission('operations:write')
  const location = useLocation()
  const mode = modeFromPath(location.pathname)

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
      setSuccessMessage(action === 'start' ? 'Season started' : 'Season ended')
      setTimeout(() => setSuccessMessage(null), 2000)
      seasonsQuery.refetch()
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
      eventsQuery.refetch()
    } catch (err) {
      setSuccessMessage(err instanceof Error ? err.message : 'Action failed')
    }
  }

  const titles: Record<OpsMode, { h1: string; sub: string }> = {
    overview: {
      h1: 'Operations overview',
      sub: 'Seasons (long-lived ranking periods) and game events (short campaigns)',
    },
    seasons: {
      h1: 'Seasons',
      sub: 'Schedule, open, and close ranked seasons and reward pools',
    },
    events: {
      h1: 'Game Events',
      sub: 'Tournaments, challenges, and limited-time promotions',
    },
  }

  const showSeasons = mode === 'overview' || mode === 'seasons'
  const showEvents = mode === 'overview' || mode === 'events'

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-8">
        <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold text-ink-primary">{titles[mode].h1}</h1>
            <p className="mt-2 text-ink-secondary">{titles[mode].sub}</p>
          </div>
          <div className="flex rounded border border-panel-border overflow-hidden text-sm">
            <Link
              to="/operations"
              className={`px-3 py-2 ${mode === 'overview' ? 'bg-accent text-white' : 'bg-bg-secondary text-ink-secondary'}`}
            >
              Overview
            </Link>
            <Link
              to="/operations/seasons"
              className={`px-3 py-2 border-l border-panel-border ${
                mode === 'seasons' ? 'bg-accent text-white' : 'bg-bg-secondary text-ink-secondary'
              }`}
            >
              Seasons
            </Link>
            <Link
              to="/operations/game-events"
              className={`px-3 py-2 border-l border-panel-border ${
                mode === 'events' ? 'bg-accent text-white' : 'bg-bg-secondary text-ink-secondary'
              }`}
            >
              Events
            </Link>
          </div>
        </div>

        {statsQuery.isLoading ? (
          <SkeletonGrid count={4} />
        ) : statsQuery.data ? (
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className={`operator-card ${mode === 'seasons' ? 'ring-2 ring-accent' : ''}`}>
              <p className="text-xs text-ink-tertiary">Active Seasons</p>
              <p className="text-2xl font-bold text-accent mt-1">{statsQuery.data.activeSeasons}</p>
            </div>
            <div className={`operator-card ${mode === 'events' ? 'ring-2 ring-accent' : ''}`}>
              <p className="text-xs text-ink-tertiary">Upcoming Events</p>
              <p className="text-2xl font-bold text-accent mt-1">{statsQuery.data.upcomingEvents}</p>
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
        ) : null}

        {successMessage && (
          <div className="p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm">
            ✓ {successMessage}
          </div>
        )}

        {showSeasons && (
          <div className={mode === 'seasons' ? 'space-y-4' : 'space-y-4 opacity-100'}>
            <div className="flex items-center justify-between mb-2">
              <div>
                <h2 className="text-xl font-semibold text-ink-primary flex items-center gap-2">
                  <span className="inline-block w-1 h-6 bg-accent rounded" />
                  Seasons
                </h2>
                <p className="text-xs text-ink-tertiary mt-1">
                  Multi-week ranking periods with points multipliers and season rewards
                </p>
              </div>
              <div className="flex gap-2">
                {['draft', 'scheduled', 'active', 'ended'].map((status) => (
                  <button
                    key={status}
                    type="button"
                    onClick={() =>
                      setSeasonFilters({
                        status: seasonFilters.status === status ? undefined : (status as SeasonFilter['status']),
                      })
                    }
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
              <EmptyState
                title="No seasons found"
                description="Create seasons to manage ranked progression"
                icon="🎮"
              />
            )}
          </div>
        )}

        {showEvents && (
          <div className={mode === 'events' ? 'space-y-4' : 'space-y-4 pt-4 border-t border-panel-border'}>
            <div className="flex items-center justify-between mb-2">
              <div>
                <h2 className="text-xl font-semibold text-ink-primary flex items-center gap-2">
                  <span className="inline-block w-1 h-6 bg-status-degraded rounded" />
                  Game Events
                </h2>
                <p className="text-xs text-ink-tertiary mt-1">
                  Short-lived tournaments and challenges with capacity and prize pools
                </p>
              </div>
              <div className="flex gap-2 overflow-x-auto">
                {['draft', 'upcoming', 'active', 'ended', 'cancelled'].map((status) => (
                  <button
                    key={status}
                    type="button"
                    onClick={() =>
                      setEventFilters({
                        status: eventFilters.status === status ? undefined : (status as EventFilter['status']),
                      })
                    }
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
              <EmptyState
                title="No events found"
                description="Create game events to engage players"
                icon="🎉"
              />
            )}
          </div>
        )}
      </div>
    </ErrorBoundary>
  )
}
