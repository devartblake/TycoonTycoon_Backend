/**
 * Security Audit + IP Map page
 */

import { useState } from 'react'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons'
import { FilterBar } from '../components/filter-bar'
import { EventsTable } from '../components/events-table'
import { IPMap } from '../components/ip-map'
import {
  useAuditEvents,
  useAuditStats,
  useIPLocations,
  useEventLocations,
} from '../hooks/useAuditEvents'
import type { AuditFilter } from '../types'

export default function SecurityAuditPage() {
  usePermission('audit:read')

  const [filters, setFilters] = useState<AuditFilter>({})
  const [offset, setOffset] = useState(0)
  const limit = 50

  const eventsQuery = useAuditEvents(filters, offset, limit)
  const statsQuery = useAuditStats()
  const ipLocationsQuery = useIPLocations(filters)
  const eventLocations = useEventLocations(eventsQuery.data?.items)

  const handleFiltersChange = (newFilters: AuditFilter) => {
    setFilters(newFilters)
    setOffset(0) // Reset pagination when filters change
  }

  const handleEventClick = (event: any) => {
    // Could navigate to event detail or open a modal
    console.log('Event clicked:', event)
  }

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-8">
        {/* Header */}
        <div>
          <h1 className="text-3xl font-bold text-ink-primary">Security Audit</h1>
          <p className="mt-2 text-ink-secondary">Monitor administrative actions and access patterns</p>
        </div>

        {/* Stats */}
        {statsQuery.isLoading ? (
          <SkeletonGrid count={4} />
        ) : statsQuery.data ? (
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Total Events</p>
              <p className="text-2xl font-bold text-accent mt-1">{statsQuery.data.totalEvents.toLocaleString()}</p>
            </div>
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Success Rate</p>
              <p className="text-2xl font-bold text-status-healthy mt-1">
                {Math.round(statsQuery.data.successRate * 100)}%
              </p>
            </div>
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Admin Accounts</p>
              <p className="text-2xl font-bold text-accent mt-1">{statsQuery.data.uniqueAdmins}</p>
            </div>
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Unique IPs</p>
              <p className="text-2xl font-bold text-accent mt-1">{statsQuery.data.uniqueIPs}</p>
            </div>
          </div>
        ) : null}

      {/* Main Content */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Left Sidebar - Filters */}
        <div>
          <FilterBar filters={filters} onFiltersChange={handleFiltersChange} />
        </div>

        {/* Right Content - Map and Table */}
        <div className="lg:col-span-3 space-y-6">
          {/* Map */}
          <IPMap
            locations={eventLocations.length > 0 ? eventLocations : ipLocationsQuery.data || []}
            isLoading={ipLocationsQuery.isLoading || eventsQuery.isLoading}
          />

          {/* Events Table */}
          <div>
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-ink-primary">Recent Events</h2>
              {eventsQuery.data && (
                <p className="text-sm text-ink-secondary">
                  Showing {eventsQuery.data.items.length} of {eventsQuery.data.total} events
                </p>
              )}
            </div>
            {eventsQuery.isLoading ? (
              <SkeletonTable rows={10} columns={4} />
            ) : eventsQuery.data && eventsQuery.data.items.length > 0 ? (
              <EventsTable
                events={eventsQuery.data.items}
                isLoading={false}
                onEventClick={handleEventClick}
              />
            ) : (
              <EmptyState
                title="No audit events found"
                description="Try adjusting your filters"
                icon="📋"
              />
            )}
          </div>

          {/* Pagination */}
          {eventsQuery.data && eventsQuery.data.total > limit && (
            <div className="flex justify-between items-center">
              <button
                onClick={() => setOffset(Math.max(0, offset - limit))}
                disabled={offset === 0}
                className="px-4 py-2 bg-bg-secondary border border-panel-border rounded text-sm hover:bg-bg-tertiary disabled:opacity-50 disabled:cursor-not-allowed"
              >
                ← Previous
              </button>
              <p className="text-sm text-ink-secondary">
                Page {Math.floor(offset / limit) + 1} of {Math.ceil(eventsQuery.data.total / limit)}
              </p>
              <button
                onClick={() => setOffset(offset + limit)}
                disabled={offset + limit >= eventsQuery.data.total}
                className="px-4 py-2 bg-bg-secondary border border-panel-border rounded text-sm hover:bg-bg-tertiary disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Next →
              </button>
            </div>
          )}
        </div>
      </div>
      </div>
    </ErrorBoundary>
  )
}
