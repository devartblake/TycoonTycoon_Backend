/**
 * Store Analytics page — purchase totals, top SKUs, and stock-reset activity.
 */

import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons'
import { getPurchaseAnalytics, getStockResetAnalytics, getStoreStats } from '../api'

export default function StoreAnalyticsPage() {
  usePermission('storage:read')

  const [resetOffset, setResetOffset] = useState(0)
  const resetLimit = 25

  const purchasesQuery = useQuery({ queryKey: ['store-purchase-analytics'], queryFn: () => getPurchaseAnalytics() })
  const statsQuery = useQuery({ queryKey: ['store-stats'], queryFn: getStoreStats })
  const resetsQuery = useQuery({
    queryKey: ['store-stock-resets', resetOffset],
    queryFn: () => getStockResetAnalytics(undefined, resetOffset, resetLimit),
  })

  const purchases = purchasesQuery.data
  const resets = resetsQuery.data
  const totalResetPages = resets ? Math.max(1, Math.ceil(resets.total / resetLimit)) : 1
  const resetPage = Math.floor(resetOffset / resetLimit) + 1

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-ink-primary">Store Analytics</h1>
          <p className="mt-2 text-ink-secondary">Purchases, top SKUs, and per-player stock resets</p>
        </div>

        {/* Stat tiles */}
        {purchasesQuery.isLoading || statsQuery.isLoading ? (
          <SkeletonGrid count={3} />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Total Purchases</p>
              <p className="text-2xl font-bold text-accent mt-1">
                {purchases ? purchases.totalPurchases.toLocaleString() : '—'}
              </p>
            </div>
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Coins Spent</p>
              <p className="text-2xl font-bold text-accent mt-1">
                {purchases ? purchases.totalCoinsSpent.toLocaleString() : '—'}
              </p>
            </div>
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Catalog Items</p>
              <p className="text-2xl font-bold text-accent mt-1">
                {statsQuery.data ? statsQuery.data.totalProducts.toLocaleString() : '—'}
              </p>
            </div>
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Top SKUs */}
          <div className="operator-card p-0">
            <h2 className="text-lg font-semibold p-4 border-b border-panel-border">Top SKUs</h2>
            {purchasesQuery.isLoading ? (
              <div className="p-4"><SkeletonTable rows={5} columns={2} /></div>
            ) : purchasesQuery.isError ? (
              <div className="p-6">
                <EmptyState title="Failed to load purchases" description={(purchasesQuery.error as Error)?.message} />
              </div>
            ) : purchases && purchases.topSkus.length > 0 ? (
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs text-ink-tertiary border-b border-panel-border">
                    <th className="p-3">SKU</th>
                    <th className="p-3 text-right">Purchases</th>
                  </tr>
                </thead>
                <tbody>
                  {purchases.topSkus.map((s) => (
                    <tr key={s.sku} className="border-b border-panel-border last:border-0">
                      <td className="p-3 font-mono text-xs">{s.sku}</td>
                      <td className="p-3 text-right font-medium">{s.purchaseCount.toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <div className="p-6">
                <EmptyState title="No purchases yet" description="No store purchases recorded in this period." />
              </div>
            )}
          </div>

          {/* Stock resets */}
          <div className="operator-card p-0">
            <h2 className="text-lg font-semibold p-4 border-b border-panel-border">Recent Stock Resets</h2>
            {resetsQuery.isLoading ? (
              <div className="p-4"><SkeletonTable rows={5} columns={4} /></div>
            ) : resetsQuery.isError ? (
              <div className="p-6">
                <EmptyState title="Failed to load resets" description={(resetsQuery.error as Error)?.message} />
              </div>
            ) : resets && resets.items.length > 0 ? (
              <>
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-left text-xs text-ink-tertiary border-b border-panel-border">
                      <th className="p-3">Player</th>
                      <th className="p-3">SKU</th>
                      <th className="p-3">Used</th>
                      <th className="p-3">Last reset</th>
                    </tr>
                  </thead>
                  <tbody>
                    {resets.items.map((r, i) => (
                      <tr key={`${r.playerId}-${r.sku}-${i}`} className="border-b border-panel-border last:border-0">
                        <td className="p-3 font-mono text-xs">{r.playerId}</td>
                        <td className="p-3 font-mono text-xs">{r.sku}</td>
                        <td className="p-3">{r.quantityUsed}</td>
                        <td className="p-3 text-ink-tertiary">
                          {r.lastResetAt ? new Date(r.lastResetAt).toLocaleString() : '—'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                <div className="flex items-center justify-between p-3 border-t border-panel-border text-sm">
                  <span className="text-ink-tertiary">page {resetPage} of {totalResetPages}</span>
                  <div className="flex gap-2">
                    <button
                      onClick={() => setResetOffset(Math.max(0, resetOffset - resetLimit))}
                      disabled={resetOffset === 0}
                      className="px-3 py-1 border border-panel-border rounded disabled:opacity-40"
                    >
                      Previous
                    </button>
                    <button
                      onClick={() => setResetOffset(resetOffset + resetLimit)}
                      disabled={resetPage >= totalResetPages}
                      className="px-3 py-1 border border-panel-border rounded disabled:opacity-40"
                    >
                      Next
                    </button>
                  </div>
                </div>
              </>
            ) : (
              <div className="p-6">
                <EmptyState title="No stock resets" description="No per-player stock resets recorded." />
              </div>
            )}
          </div>
        </div>
      </div>
    </ErrorBoundary>
  )
}
