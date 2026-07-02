/**
 * Player economy summary card
 */

import type { PlayerEconomy } from '../types'

interface EconomySummaryProps {
  economy: PlayerEconomy | undefined
  isLoading: boolean
}

export function EconomySummary({ economy, isLoading }: EconomySummaryProps) {
  if (isLoading) {
    return (
      <div className="operator-card space-y-3">
        {[...Array(3)].map((_, i) => (
          <div key={i} className="h-16 bg-bg-secondary rounded animate-pulse" />
        ))}
      </div>
    )
  }

  if (!economy) {
    return null
  }

  const netEarnings = economy.totalEarned - economy.totalSpent

  return (
    <div className="operator-card space-y-6">
      {/* Current Balance */}
      <div className="pb-6 border-b border-panel-border">
        <p className="text-xs text-ink-tertiary">Current Balance</p>
        <p className="text-4xl font-bold text-accent mt-2">
          {economy.currentBalance.toLocaleString()}
        </p>
      </div>

      {/* Earnings and Spending */}
      <div className="grid grid-cols-3 gap-4">
        <div>
          <p className="text-xs text-ink-tertiary">Total Earned</p>
          <p className="text-lg font-bold text-status-healthy mt-1">
            +{economy.totalEarned.toLocaleString()}
          </p>
        </div>
        <div>
          <p className="text-xs text-ink-tertiary">Total Spent</p>
          <p className="text-lg font-bold text-status-offline mt-1">
            -{economy.totalSpent.toLocaleString()}
          </p>
        </div>
        <div>
          <p className="text-xs text-ink-tertiary">Net</p>
          <p className={`text-lg font-bold mt-1 ${netEarnings >= 0 ? 'text-status-healthy' : 'text-status-offline'}`}>
            {netEarnings >= 0 ? '+' : ''}{netEarnings.toLocaleString()}
          </p>
        </div>
      </div>

      {/* Refunds and Activity */}
      <div className="pt-4 border-t border-panel-border space-y-2">
        {economy.totalRefunded > 0 && (
          <p className="text-sm text-ink-secondary">
            Total Refunded: {economy.totalRefunded.toLocaleString()}
          </p>
        )}
        <p className="text-xs text-ink-tertiary">
          Account Created: {new Date(economy.accountCreatedAt).toLocaleDateString()}
        </p>
        {economy.lastTransactionAt && (
          <p className="text-xs text-ink-tertiary">
            Last Transaction: {new Date(economy.lastTransactionAt).toLocaleDateString()}
          </p>
        )}
      </div>
    </div>
  )
}
