/**
 * Reconciliation issues — persisted findings from the daily payment
 * reconciliation job. Operators resolve them (with notes) once investigated.
 * Backs GET /admin/payment-reconciliation/issues + /{id}/resolve.
 */

import { useState } from 'react'
import { Link } from 'react-router-dom'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonTable } from '@/components/shared/skeletons'
import { Button } from '@/components/ui/button'
import { usePaymentIssues, useResolveIssue } from '../hooks/usePayments'
import type { PaymentIssue } from '../types'

export default function ReconciliationPage() {
  usePermission('economy:read')

  const [showResolved, setShowResolved] = useState(false)
  const [page, setPage] = useState(1)
  const [resolveTarget, setResolveTarget] = useState<PaymentIssue | null>(null)
  const [notes, setNotes] = useState('')
  const [flash, setFlash] = useState<string | null>(null)

  const pageSize = 20
  const query = usePaymentIssues({ resolved: showResolved, page, pageSize })
  const resolve = useResolveIssue()

  const items = query.data?.items ?? []
  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / pageSize))

  const submitResolve = async () => {
    if (!resolveTarget) return
    await resolve.mutateAsync({ id: resolveTarget.id, notes: notes.trim() || undefined })
    setResolveTarget(null)
    setNotes('')
    setFlash('Issue resolved.')
    setTimeout(() => setFlash(null), 3500)
  }

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-6">
        <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold text-ink-primary">Reconciliation issues</h1>
            <p className="mt-2 text-ink-secondary">
              Mismatches the daily reconciliation job flagged for operator action.
            </p>
          </div>
          <Link to="/payments" className="text-sm text-accent hover:underline self-start sm:self-auto">
            ← Payments
          </Link>
        </div>

        {flash && (
          <div className="p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm">
            ✓ {flash}
          </div>
        )}

        <div className="operator-card flex items-center gap-3">
          <label className="flex items-center gap-2 text-sm text-ink-secondary">
            <input
              type="checkbox"
              checked={showResolved}
              onChange={(e) => {
                setShowResolved(e.target.checked)
                setPage(1)
              }}
            />
            Show resolved
          </label>
        </div>

        {query.isLoading ? (
          <SkeletonTable rows={6} columns={5} />
        ) : items.length === 0 ? (
          <EmptyState
            title={showResolved ? 'No resolved issues' : 'No open issues'}
            description={showResolved ? 'Nothing has been resolved yet.' : 'Reconciliation is clean — no open mismatches.'}
            icon="✅"
          />
        ) : (
          <div className="operator-card overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-ink-tertiary border-b border-panel-border">
                  <th className="py-2 pr-4 font-medium">Category</th>
                  <th className="py-2 pr-4 font-medium">Provider ref</th>
                  <th className="py-2 pr-4 font-medium">Expected / Actual</th>
                  <th className="py-2 pr-4 font-medium">Created</th>
                  <th className="py-2 pr-4 font-medium text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {items.map((i) => (
                  <tr key={i.id} className="border-b border-panel-border/50 align-top">
                    <td className="py-2 pr-4">
                      <div className="text-status-warning font-medium">{i.category}</div>
                      <div className="text-ink-tertiary text-xs max-w-md">{i.details}</div>
                    </td>
                    <td className="py-2 pr-4 font-mono text-xs text-ink-secondary">
                      <div className="uppercase">{i.provider}</div>
                      <div>{i.providerRef}</div>
                    </td>
                    <td className="py-2 pr-4 text-ink-primary text-xs">
                      {i.expectedAmount != null ? i.expectedAmount.toFixed(2) : '—'}
                      {' / '}
                      {i.actualAmount != null ? i.actualAmount.toFixed(2) : '—'}
                    </td>
                    <td className="py-2 pr-4 text-ink-tertiary text-xs">{new Date(i.createdAtUtc).toLocaleString()}</td>
                    <td className="py-2 pr-0 text-right">
                      {i.resolvedAtUtc ? (
                        <span className="text-status-healthy text-xs">
                          Resolved{i.resolvedBy ? ` by ${i.resolvedBy}` : ''}
                        </span>
                      ) : (
                        <Button variant="secondary" size="sm" onClick={() => setResolveTarget(i)}>Resolve</Button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {total > pageSize && (
          <div className="flex justify-between items-center">
            <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>← Previous</Button>
            <p className="text-sm text-ink-secondary">Page {page} of {totalPages}</p>
            <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>Next →</Button>
          </div>
        )}

        {resolveTarget && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
            <div className="operator-card max-w-md w-full mx-4 space-y-4">
              <h2 className="text-lg font-semibold text-ink-primary">Resolve issue</h2>
              <div className="p-3 bg-bg-secondary rounded text-sm">
                <p className="text-status-warning font-medium">{resolveTarget.category}</p>
                <p className="text-ink-secondary text-xs mt-1">{resolveTarget.details}</p>
              </div>
              <div>
                <label htmlFor="resolve-notes" className="block text-sm font-medium text-ink-primary mb-1">
                  Resolution notes (optional)
                </label>
                <textarea
                  id="resolve-notes"
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  className="w-full px-3 py-2 border border-panel-border rounded focus-ring h-24 resize-none text-sm"
                  placeholder="What was done / why this is resolved"
                />
              </div>
              <div className="flex gap-2 pt-4 border-t border-panel-border">
                <Button onClick={submitResolve} disabled={resolve.isPending} className="flex-1">
                  {resolve.isPending ? 'Resolving...' : 'Confirm'}
                </Button>
                <Button variant="secondary" onClick={() => { setResolveTarget(null); setNotes('') }} disabled={resolve.isPending} className="flex-1">
                  Cancel
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    </ErrorBoundary>
  )
}
