/**
 * Moderation Log Detail page — full record of one moderation action.
 */

import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonGrid } from '@/components/shared/skeletons'
import { getModerationLogDetail } from '../api'

export default function ModerationLogDetailPage() {
  usePermission('users:read')

  const { logId } = useParams<{ logId: string }>()
  const navigate = useNavigate()

  const logQuery = useQuery({
    queryKey: ['moderation-log', logId],
    queryFn: () => getModerationLogDetail(logId!),
    enabled: !!logId,
  })

  if (!logId) {
    return (
      <div className="operator-container py-12">
        <EmptyState title="Log not found" description="No log id in the URL." />
      </div>
    )
  }

  const log = logQuery.data

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-6">
        <div>
          <button onClick={() => navigate(-1)} className="text-accent hover:underline text-sm mb-2">
            ← Back to logs
          </button>
          <h1 className="text-3xl font-bold text-ink-primary">Moderation Log</h1>
          <p className="mt-1 text-ink-secondary font-mono text-sm">{logId}</p>
        </div>

        {logQuery.isLoading ? (
          <SkeletonGrid count={4} />
        ) : logQuery.isError ? (
          <EmptyState title="Failed to load log" description={(logQuery.error as Error)?.message} />
        ) : log ? (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">New status</p>
              <p className="text-2xl font-bold mt-1 capitalize">{log.newStatus}</p>
              <p className="text-xs text-ink-tertiary mt-2">Applied by</p>
              <p className="text-sm text-ink-primary">{log.setByAdmin}</p>
              <p className="text-xs text-ink-tertiary mt-2">When</p>
              <p className="text-sm text-ink-primary">{new Date(log.createdAt).toLocaleString()}</p>
              {log.expiresAt && (
                <>
                  <p className="text-xs text-ink-tertiary mt-2">Expires</p>
                  <p className="text-sm text-ink-primary">{new Date(log.expiresAt).toLocaleString()}</p>
                </>
              )}
            </div>
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Player</p>
              <Link to={`/moderation/players/${log.playerId}`} className="text-accent hover:underline font-mono text-sm">
                {log.playerId}
              </Link>
              <p className="text-xs text-ink-tertiary mt-3">Reason</p>
              <p className="text-sm text-ink-primary">{log.reason}</p>
              {log.notes && (
                <>
                  <p className="text-xs text-ink-tertiary mt-3">Notes</p>
                  <p className="text-sm text-ink-primary whitespace-pre-wrap">{log.notes}</p>
                </>
              )}
              {log.relatedFlagId && (
                <>
                  <p className="text-xs text-ink-tertiary mt-3">Related anti-cheat flag</p>
                  <p className="text-sm font-mono text-ink-primary">{log.relatedFlagId}</p>
                </>
              )}
            </div>
          </div>
        ) : null}
      </div>
    </ErrorBoundary>
  )
}
