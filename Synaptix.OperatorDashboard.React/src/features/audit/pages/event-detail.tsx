/**
 * Audit Event Detail page — full record of one security-audit event,
 * including the raw metadata the backend attached to it.
 */

import { useParams, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonGrid } from '@/components/shared/skeletons'
import { getEventDetail } from '../api'

export default function AuditEventDetailPage() {
  usePermission('audit:read')

  const { eventId } = useParams<{ eventId: string }>()
  const navigate = useNavigate()

  const eventQuery = useQuery({
    queryKey: ['audit-event', eventId],
    queryFn: () => getEventDetail(eventId!),
    enabled: !!eventId,
  })

  if (!eventId) {
    return (
      <div className="operator-container py-12">
        <EmptyState title="Event not found" description="No event id in the URL." />
      </div>
    )
  }

  const event = eventQuery.data
  const detailEntries = event?.details ? Object.entries(event.details) : []

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-6">
        <div>
          <button onClick={() => navigate(-1)} className="text-accent hover:underline text-sm mb-2">
            ← Back to security audit
          </button>
          <h1 className="text-3xl font-bold text-ink-primary">Audit Event</h1>
          <p className="mt-1 text-ink-secondary font-mono text-sm">{eventId}</p>
        </div>

        {eventQuery.isLoading ? (
          <SkeletonGrid count={4} />
        ) : eventQuery.isError ? (
          <EmptyState title="Failed to load event" description={(eventQuery.error as Error)?.message} />
        ) : event ? (
          <>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="operator-card">
                <p className="text-xs text-ink-tertiary">Action</p>
                <p className="text-sm font-medium text-ink-primary mt-1">{event.action || '—'}</p>
                <p className="text-xs text-ink-tertiary mt-2">Channel</p>
                <p className="text-sm font-mono text-ink-primary">{event.resourceType}</p>
              </div>
              <div className="operator-card">
                <p className="text-xs text-ink-tertiary">Outcome</p>
                <p className={`text-2xl font-bold mt-1 capitalize ${event.status === 'success' ? 'text-status-healthy' : 'text-status-offline'}`}>
                  {event.status}
                </p>
                <p className="text-xs text-ink-tertiary mt-2">When</p>
                <p className="text-sm text-ink-primary">{new Date(event.timestamp).toLocaleString()}</p>
              </div>
              <div className="operator-card">
                <p className="text-xs text-ink-tertiary">Actor</p>
                <p className="text-sm font-medium text-ink-primary mt-1 break-all">{event.adminEmail || 'unknown'}</p>
                {event.ipAddress && (
                  <>
                    <p className="text-xs text-ink-tertiary mt-2">IP</p>
                    <p className="text-sm font-mono text-ink-primary">{event.ipAddress}</p>
                  </>
                )}
                {event.userAgent && (
                  <>
                    <p className="text-xs text-ink-tertiary mt-2">User agent</p>
                    <p className="text-xs text-ink-tertiary break-all">{event.userAgent}</p>
                  </>
                )}
              </div>
            </div>

            {/* Raw metadata */}
            <div className="operator-card p-0">
              <h2 className="text-lg font-semibold p-4 border-b border-panel-border">Metadata</h2>
              {detailEntries.length > 0 ? (
                <table className="w-full text-sm">
                  <tbody>
                    {detailEntries.map(([key, value]) => (
                      <tr key={key} className="border-b border-panel-border last:border-0">
                        <td className="p-3 font-mono text-xs text-ink-tertiary w-48 align-top">{key}</td>
                        <td className="p-3 font-mono text-xs break-all">
                          {typeof value === 'string' ? value : JSON.stringify(value)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <div className="p-6">
                  <EmptyState title="No metadata" description="This event carries no additional metadata." />
                </div>
              )}
            </div>
          </>
        ) : null}
      </div>
    </ErrorBoundary>
  )
}
