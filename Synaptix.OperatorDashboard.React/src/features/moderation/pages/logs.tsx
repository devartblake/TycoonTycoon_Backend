/**
 * Moderation Logs page — paged list of moderation actions with status/player filters.
 */

import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonTable } from '@/components/shared/skeletons'
import { getModerationLogs } from '../api'
import type { ModerationLogFilter, ModerationLogStatus } from '../types'

const STATUS_STYLES: Record<ModerationLogStatus, string> = {
  normal: 'text-status-healthy',
  suspected: 'text-status-degraded',
  restricted: 'text-status-degraded',
  banned: 'text-status-offline',
}

const STATUS_OPTIONS: ModerationLogStatus[] = ['normal', 'suspected', 'restricted', 'banned']

export default function ModerationLogsPage() {
  usePermission('users:read')

  const navigate = useNavigate()
  const [filters, setFilters] = useState<ModerationLogFilter>({})
  const [playerIdInput, setPlayerIdInput] = useState('')
  const [offset, setOffset] = useState(0)
  const limit = 25

  const logsQuery = useQuery({
    queryKey: ['moderation-logs', filters, offset],
    queryFn: () => getModerationLogs(filters, offset, limit),
  })

  const applyPlayerFilter = () => {
    setFilters((f) => ({ ...f, playerId: playerIdInput.trim() || undefined }))
    setOffset(0)
  }

  const data = logsQuery.data
  const totalPages = data ? Math.max(1, Math.ceil(data.total / limit)) : 1
  const page = Math.floor(offset / limit) + 1

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-ink-primary">Moderation Logs</h1>
          <p className="mt-2 text-ink-secondary">Every status change applied by moderators, newest first</p>
        </div>

        {/* Filters */}
        <div className="operator-card flex flex-wrap items-end gap-3">
          <div>
            <label className="block text-xs text-ink-tertiary mb-1">Status</label>
            <select
              value={filters.status ?? ''}
              onChange={(e) => {
                setFilters((f) => ({ ...f, status: (e.target.value || undefined) as ModerationLogStatus | undefined }))
                setOffset(0)
              }}
              className="px-3 py-2 border border-panel-border rounded text-sm focus-ring"
            >
              <option value="">All</option>
              {STATUS_OPTIONS.map((s) => (
                <option key={s} value={s}>{s}</option>
              ))}
            </select>
          </div>
          <div className="flex-1 min-w-[220px]">
            <label className="block text-xs text-ink-tertiary mb-1">Player ID</label>
            <input
              value={playerIdInput}
              onChange={(e) => setPlayerIdInput(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && applyPlayerFilter()}
              placeholder="Filter by player GUID"
              className="w-full px-3 py-2 border border-panel-border rounded text-sm focus-ring font-mono"
            />
          </div>
          <button
            onClick={applyPlayerFilter}
            className="px-4 py-2 bg-accent text-white rounded text-sm font-medium hover:bg-accent-dark transition-smooth"
          >
            Apply
          </button>
        </div>

        {/* Table */}
        <div className="operator-card p-0">
          {logsQuery.isLoading ? (
            <div className="p-4"><SkeletonTable rows={8} columns={5} /></div>
          ) : logsQuery.isError ? (
            <div className="p-6">
              <EmptyState title="Failed to load logs" description={(logsQuery.error as Error)?.message} />
            </div>
          ) : data && data.items.length > 0 ? (
            <>
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs text-ink-tertiary border-b border-panel-border">
                    <th className="p-3">Status</th>
                    <th className="p-3">Player</th>
                    <th className="p-3">Reason</th>
                    <th className="p-3">By</th>
                    <th className="p-3">When</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((log) => (
                    <tr
                      key={log.id}
                      onClick={() => navigate(`/moderation/logs/${log.id}`)}
                      className="border-b border-panel-border last:border-0 cursor-pointer hover:bg-bg-secondary transition-smooth"
                    >
                      <td className={`p-3 font-medium capitalize ${STATUS_STYLES[log.newStatus]}`}>{log.newStatus}</td>
                      <td className="p-3">
                        <Link
                          to={`/moderation/players/${log.playerId}`}
                          onClick={(e) => e.stopPropagation()}
                          className="text-accent hover:underline font-mono text-xs"
                        >
                          {log.playerId}
                        </Link>
                      </td>
                      <td className="p-3">{log.reason}</td>
                      <td className="p-3 text-ink-tertiary">{log.setByAdmin}</td>
                      <td className="p-3 text-ink-tertiary">{new Date(log.createdAt).toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {/* Pagination */}
              <div className="flex items-center justify-between p-3 border-t border-panel-border text-sm">
                <span className="text-ink-tertiary">
                  {data.total.toLocaleString()} entries · page {page} of {totalPages}
                </span>
                <div className="flex gap-2">
                  <button
                    onClick={() => setOffset(Math.max(0, offset - limit))}
                    disabled={offset === 0}
                    className="px-3 py-1 border border-panel-border rounded disabled:opacity-40"
                  >
                    Previous
                  </button>
                  <button
                    onClick={() => setOffset(offset + limit)}
                    disabled={page >= totalPages}
                    className="px-3 py-1 border border-panel-border rounded disabled:opacity-40"
                  >
                    Next
                  </button>
                </div>
              </div>
            </>
          ) : (
            <div className="p-6">
              <EmptyState title="No moderation logs" description="No actions match the current filters." />
            </div>
          )}
        </div>
      </div>
    </ErrorBoundary>
  )
}
