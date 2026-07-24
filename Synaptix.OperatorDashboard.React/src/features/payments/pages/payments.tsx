/**
 * Payments — operator view of PayPal/Stripe checkout attempts with manual
 * reconcile, retry-fulfillment, and refund. Backs AdminPaymentsEndpoints.
 */

import { Fragment, useState } from 'react'
import { Link } from 'react-router-dom'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonTable } from '@/components/shared/skeletons'
import { Button } from '@/components/ui/button'
import {
  usePayments,
  usePaymentDetail,
  useReconcilePayment,
  useRetryFulfillment,
  useRefundPayment,
} from '../hooks/usePayments'
import { RefundModal } from '../components/refund-modal'
import type { PaymentAttempt, PaymentStatus } from '../types'

const STATUS_STYLES: Record<string, string> = {
  Created: 'bg-status-warning/10 text-status-warning',
  Captured: 'bg-status-healthy/10 text-status-healthy',
  Failed: 'bg-status-offline/10 text-status-offline',
  Expired: 'bg-bg-tertiary text-ink-tertiary',
  Refunded: 'bg-accent/10 text-accent',
}

function StatusBadge({ status }: { status: string }) {
  return (
    <span className={`inline-block px-2 py-0.5 rounded text-xs font-medium ${STATUS_STYLES[status] ?? 'bg-bg-tertiary text-ink-secondary'}`}>
      {status}
    </span>
  )
}

const STATUS_OPTIONS: PaymentStatus[] = ['Created', 'Captured', 'Failed', 'Expired', 'Refunded']

export default function PaymentsPage() {
  usePermission('economy:read')

  const [provider, setProvider] = useState('')
  const [status, setStatus] = useState('')
  const [playerId, setPlayerId] = useState('')
  const [page, setPage] = useState(1)
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const [refundTarget, setRefundTarget] = useState<PaymentAttempt | null>(null)
  const [flash, setFlash] = useState<string | null>(null)

  const pageSize = 20
  const query = usePayments({ provider, status, playerId, page, pageSize })
  const detail = usePaymentDetail(expandedId)
  const reconcile = useReconcilePayment()
  const retry = useRetryFulfillment()
  const refund = useRefundPayment()

  const items = query.data?.items ?? []
  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / pageSize))

  const showFlash = (msg: string) => {
    setFlash(msg)
    setTimeout(() => setFlash(null), 3500)
  }

  const onReconcile = async (id: string) => {
    const res = await reconcile.mutateAsync(id)
    showFlash(res.issueRaised ? `Reconciled — issue raised (status ${res.status}).` : `Reconciled — no issue (status ${res.status}).`)
  }

  const onRetry = async (id: string) => {
    const res = await retry.mutateAsync(id)
    showFlash(`Retry fulfillment: ${res.status}.`)
  }

  const onRefund = async (reason: string, amount?: number) => {
    if (!refundTarget) return
    const res = await refund.mutateAsync({ id: refundTarget.id, reason, amount })
    showFlash(`Refund ${res.refundStatus} (${res.isFullRefund ? 'full' : 'partial'}).`)
  }

  const applyFilters = () => setPage(1)

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-6">
        <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold text-ink-primary">Payments</h1>
            <p className="mt-2 text-ink-secondary">
              PayPal &amp; Stripe checkout attempts — reconcile, retry fulfillment, and refund.
            </p>
          </div>
          <Link to="/payments/reconciliation" className="text-sm text-accent hover:underline self-start sm:self-auto">
            Reconciliation issues →
          </Link>
        </div>

        {flash && (
          <div className="p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm">
            ✓ {flash}
          </div>
        )}

        {/* Filters */}
        <div className="operator-card flex flex-wrap items-end gap-3">
          <div>
            <label className="block text-xs text-ink-tertiary mb-1">Provider</label>
            <select value={provider} onChange={(e) => setProvider(e.target.value)} className="px-3 py-2 border border-panel-border rounded bg-bg-primary text-sm">
              <option value="">All</option>
              <option value="paypal">PayPal</option>
              <option value="stripe">Stripe</option>
            </select>
          </div>
          <div>
            <label className="block text-xs text-ink-tertiary mb-1">Status</label>
            <select value={status} onChange={(e) => setStatus(e.target.value)} className="px-3 py-2 border border-panel-border rounded bg-bg-primary text-sm">
              <option value="">All</option>
              {STATUS_OPTIONS.map((s) => (
                <option key={s} value={s}>{s}</option>
              ))}
            </select>
          </div>
          <div className="flex-1 min-w-[220px]">
            <label className="block text-xs text-ink-tertiary mb-1">Player ID</label>
            <input value={playerId} onChange={(e) => setPlayerId(e.target.value)} placeholder="UUID" className="w-full px-3 py-2 border border-panel-border rounded focus-ring text-sm font-mono" />
          </div>
          <Button variant="secondary" size="sm" onClick={applyFilters}>Apply</Button>
        </div>

        {query.isLoading ? (
          <SkeletonTable rows={8} columns={6} />
        ) : items.length === 0 ? (
          <EmptyState title="No payments found" description="No checkout attempts match these filters." icon="💳" />
        ) : (
          <div className="operator-card overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-ink-tertiary border-b border-panel-border">
                  <th className="py-2 pr-4 font-medium">Provider</th>
                  <th className="py-2 pr-4 font-medium">Item</th>
                  <th className="py-2 pr-4 font-medium">Amount</th>
                  <th className="py-2 pr-4 font-medium">Status</th>
                  <th className="py-2 pr-4 font-medium">Created</th>
                  <th className="py-2 pr-4 font-medium text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {items.map((a) => {
                  const busy = reconcile.isPending || retry.isPending || refund.isPending
                  const isOpen = expandedId === a.id
                  return (
                    <Fragment key={a.id}>
                      <tr className="border-b border-panel-border/50 align-top">
                        <td className="py-2 pr-4 uppercase text-ink-secondary">{a.provider}</td>
                        <td className="py-2 pr-4">
                          <div className="text-ink-primary font-mono text-xs">{a.sku}</div>
                          <div className="text-ink-tertiary text-xs">×{a.quantity}</div>
                        </td>
                        <td className="py-2 pr-4 text-ink-primary">{a.expectedAmount.toFixed(2)} {a.currency}</td>
                        <td className="py-2 pr-4"><StatusBadge status={a.status} /></td>
                        <td className="py-2 pr-4 text-ink-tertiary text-xs">{new Date(a.createdAtUtc).toLocaleString()}</td>
                        <td className="py-2 pr-0">
                          <div className="flex gap-2 justify-end flex-wrap">
                            <Button variant="ghost" size="sm" onClick={() => setExpandedId(isOpen ? null : a.id)}>
                              {isOpen ? 'Hide' : 'View'}
                            </Button>
                            <Button variant="secondary" size="sm" disabled={busy} onClick={() => onReconcile(a.id)}>Reconcile</Button>
                            <Button variant="secondary" size="sm" disabled={busy} onClick={() => onRetry(a.id)}>Retry</Button>
                            <Button variant="destructive" size="sm" disabled={busy || a.status !== 'Captured'} onClick={() => setRefundTarget(a)}>Refund</Button>
                          </div>
                        </td>
                      </tr>
                      {isOpen && (
                        <tr className="border-b border-panel-border/50 bg-bg-secondary/40">
                          <td colSpan={6} className="py-3 px-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
                              <div className="space-y-1">
                                <p className="text-ink-tertiary">Player</p>
                                <p className="font-mono text-ink-primary">{a.playerId}</p>
                                <p className="text-ink-tertiary mt-2">Provider ref</p>
                                <p className="font-mono text-ink-primary">{a.providerRef}</p>
                                {a.providerCaptureRef && (
                                  <>
                                    <p className="text-ink-tertiary mt-2">Capture ref</p>
                                    <p className="font-mono text-ink-primary">{a.providerCaptureRef}</p>
                                  </>
                                )}
                                {a.playerTransactionId && (
                                  <>
                                    <p className="text-ink-tertiary mt-2">Fulfillment tx</p>
                                    <p className="font-mono text-ink-primary">{a.playerTransactionId}</p>
                                  </>
                                )}
                              </div>
                              <div>
                                <p className="text-ink-tertiary mb-1">Reconciliation issues</p>
                                {detail.isLoading ? (
                                  <p className="text-ink-tertiary">Loading…</p>
                                ) : (detail.data?.issues.length ?? 0) === 0 ? (
                                  <p className="text-status-healthy">None</p>
                                ) : (
                                  <ul className="space-y-1">
                                    {detail.data?.issues.map((i) => (
                                      <li key={i.id} className="text-ink-secondary">
                                        <span className="text-status-warning font-medium">{i.category}</span> — {i.details}
                                        {i.resolvedAtUtc ? ' (resolved)' : ' (open)'}
                                      </li>
                                    ))}
                                  </ul>
                                )}
                              </div>
                            </div>
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  )
                })}
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

        <RefundModal
          attempt={refundTarget}
          isOpen={refundTarget !== null}
          isLoading={refund.isPending}
          onClose={() => setRefundTarget(null)}
          onSubmit={onRefund}
        />
      </div>
    </ErrorBoundary>
  )
}
