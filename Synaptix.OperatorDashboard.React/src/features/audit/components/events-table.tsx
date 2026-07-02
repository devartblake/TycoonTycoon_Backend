/**
 * Audit events table
 */

import { formatDateTime } from '@/lib/utils'
import type { AuditEvent } from '../types'

interface EventsTableProps {
  events: AuditEvent[]
  isLoading: boolean
  onEventClick?: (event: AuditEvent) => void
}

const EVENT_TYPE_LABELS: Record<AuditEvent['eventType'], string> = {
  login: 'Login',
  api_call: 'API Call',
  permission_change: 'Permission Change',
  data_export: 'Data Export',
  deletion: 'Deletion',
  configuration_change: 'Configuration Change',
}

const STATUS_COLOR = {
  success: 'bg-status-healthy/10 text-status-healthy',
  failure: 'bg-status-offline/10 text-status-offline',
}

export function EventsTable({ events, isLoading, onEventClick }: EventsTableProps) {
  if (isLoading) {
    return (
      <div className="operator-card space-y-2">
        {[...Array(5)].map((_, i) => (
          <div key={i} className="h-12 bg-bg-secondary rounded animate-pulse" />
        ))}
      </div>
    )
  }

  if (events.length === 0) {
    return (
      <div className="text-center py-12 text-ink-secondary operator-card">
        <p>No events found</p>
      </div>
    )
  }

  return (
    <div className="operator-card overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-panel-border">
            <th className="px-4 py-2 text-left text-xs font-semibold text-ink-tertiary">Timestamp</th>
            <th className="px-4 py-2 text-left text-xs font-semibold text-ink-tertiary">Admin</th>
            <th className="px-4 py-2 text-left text-xs font-semibold text-ink-tertiary">Event Type</th>
            <th className="px-4 py-2 text-left text-xs font-semibold text-ink-tertiary">Resource</th>
            <th className="px-4 py-2 text-left text-xs font-semibold text-ink-tertiary">IP / Location</th>
            <th className="px-4 py-2 text-center text-xs font-semibold text-ink-tertiary">Status</th>
          </tr>
        </thead>
        <tbody>
          {events.map((event) => (
            <tr
              key={event.id}
              onClick={() => onEventClick?.(event)}
              className="border-b border-panel-border hover:bg-bg-secondary transition-colors cursor-pointer"
            >
              <td className="px-4 py-2 text-ink-primary">{formatDateTime(event.timestamp)}</td>
              <td className="px-4 py-2 text-ink-secondary text-xs">{event.adminEmail}</td>
              <td className="px-4 py-2">
                <span className="px-2 py-1 bg-bg-secondary rounded text-xs text-ink-primary">
                  {EVENT_TYPE_LABELS[event.eventType]}
                </span>
              </td>
              <td className="px-4 py-2 text-ink-secondary text-xs">
                {event.resourceType} / {event.resourceId}
              </td>
              <td className="px-4 py-2 text-ink-secondary text-xs">
                {event.ipAddress}
                {event.city && ` (${event.city}, ${event.country})`}
              </td>
              <td className="px-4 py-2 text-center">
                <span className={`inline-block px-2 py-1 rounded text-xs font-medium ${STATUS_COLOR[event.status]}`}>
                  {event.status === 'success' ? '✓' : '✕'} {event.status}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
