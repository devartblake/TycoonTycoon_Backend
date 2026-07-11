/**
 * Store Player Stock page — look up a player's per-SKU purchase limits and
 * apply per-player overrides.
 */

import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonTable } from '@/components/shared/skeletons'
import { getPlayerStock, overridePlayerStock } from '../api'
import { searchPlayers } from '@/features/economy/api'

export default function PlayerStockPage() {
  usePermission('storage:read')

  const queryClient = useQueryClient()
  const [searchInput, setSearchInput] = useState('')
  const [playerId, setPlayerId] = useState('')
  const [overrideSku, setOverrideSku] = useState<string | null>(null)
  const [overrideValue, setOverrideValue] = useState('')
  const [message, setMessage] = useState<string | null>(null)

  const stockQuery = useQuery({
    queryKey: ['player-stock', playerId],
    queryFn: () => getPlayerStock(playerId),
    enabled: !!playerId,
    retry: false,
  })

  const overrideMutation = useMutation({
    mutationFn: ({ sku, value }: { sku: string; value: number | null }) =>
      overridePlayerStock(playerId, sku, value, 'operator dashboard override'),
    onSuccess: (_res, vars) => {
      setMessage(vars.value === null ? `Override cleared for ${vars.sku}` : `Override applied to ${vars.sku}`)
      setOverrideSku(null)
      setOverrideValue('')
      queryClient.invalidateQueries({ queryKey: ['player-stock', playerId] })
    },
  })

  const resolvePlayer = async () => {
    const q = searchInput.trim()
    if (!q) return
    setMessage(null)
    // A raw GUID (with or without hyphens) can be used directly; anything else
    // goes through the player-lookup resolver (email/handle/short code).
    if (/^[0-9a-f-]{32,36}$/i.test(q)) {
      setPlayerId(q)
      return
    }
    const matches = await searchPlayers(q)
    if (matches.length > 0) {
      setPlayerId(matches[0].playerId)
    } else {
      setPlayerId('')
      setMessage(`No player found for "${q}"`)
    }
  }

  const stock = stockQuery.data

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-ink-primary">Player Stock</h1>
          <p className="mt-2 text-ink-secondary">Per-player purchase limits and remaining stock by SKU</p>
        </div>

        {/* Player lookup */}
        <div className="operator-card flex flex-wrap items-end gap-3">
          <div className="flex-1 min-w-[260px]">
            <label className="block text-xs text-ink-tertiary mb-1">Player (GUID, email, handle, or short code)</label>
            <input
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && resolvePlayer()}
              placeholder="e.g. 8f14e45f-… or player@example.com"
              className="w-full px-3 py-2 border border-panel-border rounded text-sm focus-ring font-mono"
            />
          </div>
          <button
            onClick={resolvePlayer}
            className="px-4 py-2 bg-accent text-white rounded text-sm font-medium hover:bg-accent-dark transition-smooth"
          >
            Look up
          </button>
        </div>

        {message && (
          <div className="p-3 bg-bg-secondary border border-panel-border rounded text-sm text-ink-secondary">
            {message}
          </div>
        )}

        {/* Stock table */}
        {!playerId ? (
          <EmptyState title="No player selected" description="Look up a player to see their per-SKU stock state." icon="🔍" />
        ) : stockQuery.isLoading ? (
          <div className="operator-card"><SkeletonTable rows={4} columns={6} /></div>
        ) : stockQuery.isError ? (
          <EmptyState title="Failed to load stock" description={(stockQuery.error as Error)?.message} />
        ) : stock && stock.items.length > 0 ? (
          <div className="operator-card p-0">
            <div className="p-4 border-b border-panel-border">
              <h2 className="text-lg font-semibold">Stock for <span className="font-mono text-sm">{stock.playerId}</span></h2>
            </div>
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-xs text-ink-tertiary border-b border-panel-border">
                  <th className="p-3">SKU</th>
                  <th className="p-3">Used</th>
                  <th className="p-3">Max</th>
                  <th className="p-3">Remaining</th>
                  <th className="p-3">Next reset</th>
                  <th className="p-3">Override</th>
                </tr>
              </thead>
              <tbody>
                {stock.items.map((item) => (
                  <tr key={item.sku} className="border-b border-panel-border last:border-0">
                    <td className="p-3 font-mono text-xs">{item.sku}</td>
                    <td className="p-3">{item.quantityUsed}</td>
                    <td className="p-3">
                      {item.maxQuantity}
                      {item.effectiveMaxQuantity !== null && (
                        <span className="ml-1 text-xs text-status-degraded">(override: {item.effectiveMaxQuantity})</span>
                      )}
                    </td>
                    <td className={`p-3 font-medium ${item.remaining === 0 ? 'text-status-offline' : 'text-status-healthy'}`}>
                      {item.remaining < 0 ? '∞' : item.remaining}
                    </td>
                    <td className="p-3 text-ink-tertiary">
                      {item.nextResetAtUtc ? new Date(item.nextResetAtUtc).toLocaleString() : '—'}
                    </td>
                    <td className="p-3">
                      {overrideSku === item.sku ? (
                        <div className="flex items-center gap-2">
                          <input
                            value={overrideValue}
                            onChange={(e) => setOverrideValue(e.target.value)}
                            placeholder="max"
                            className="w-20 px-2 py-1 border border-panel-border rounded text-xs focus-ring"
                          />
                          <button
                            onClick={() => {
                              const parsed = parseInt(overrideValue, 10)
                              if (!Number.isNaN(parsed) && parsed >= 0) {
                                overrideMutation.mutate({ sku: item.sku, value: parsed })
                              }
                            }}
                            disabled={overrideMutation.isPending}
                            className="text-xs text-accent hover:underline disabled:opacity-50"
                          >
                            Save
                          </button>
                          <button onClick={() => setOverrideSku(null)} className="text-xs text-ink-tertiary hover:underline">
                            Cancel
                          </button>
                        </div>
                      ) : (
                        <div className="flex items-center gap-2">
                          <button
                            onClick={() => { setOverrideSku(item.sku); setOverrideValue(String(item.effectiveMaxQuantity ?? item.maxQuantity)) }}
                            className="text-xs text-accent hover:underline"
                          >
                            Set
                          </button>
                          {item.effectiveMaxQuantity !== null && (
                            <button
                              onClick={() => overrideMutation.mutate({ sku: item.sku, value: null })}
                              disabled={overrideMutation.isPending}
                              className="text-xs text-status-offline hover:underline disabled:opacity-50"
                            >
                              Clear
                            </button>
                          )}
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {overrideMutation.isError && (
              <p className="p-3 text-xs text-status-offline">{(overrideMutation.error as Error)?.message}</p>
            )}
          </div>
        ) : (
          <EmptyState title="No stock state" description="This player has no per-SKU stock records yet." />
        )}
      </div>
    </ErrorBoundary>
  )
}
