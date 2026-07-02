/**
 * Player transactions table
 */

import { formatDateTime } from '@/lib/utils'
import type { Transaction } from '../types'

interface TransactionsTableProps {
  transactions: Transaction[]
  isLoading: boolean
  onRefundClick?: (transaction: Transaction) => void
}

const TYPE_CONFIG = {
  purchase: { icon: '🛒', color: 'text-status-offline', label: 'Purchase' },
  earn: { icon: '📈', color: 'text-status-healthy', label: 'Earn' },
  refund: { icon: '↩️', color: 'text-accent', label: 'Refund' },
  adjustment: { icon: '⚙️', color: 'text-ink-secondary', label: 'Adjustment' },
  reward: { icon: '🎁', color: 'text-status-healthy', label: 'Reward' },
  penalty: { icon: '⚠️', color: 'text-status-offline', label: 'Penalty' },
}

const STATUS_CONFIG = {
  completed: { color: 'text-status-healthy', bg: 'bg-status-healthy/10' },
  pending: { color: 'text-status-degraded', bg: 'bg-status-degraded/10' },
  failed: { color: 'text-status-offline', bg: 'bg-status-offline/10' },
  reversed: { color: 'text-ink-secondary', bg: 'bg-ink-secondary/10' },
}

export function TransactionsTable({ transactions, isLoading, onRefundClick }: TransactionsTableProps) {
  if (isLoading) {
    return (
      <div className="operator-card space-y-2">
        {[...Array(5)].map((_, i) => (
          <div key={i} className="h-12 bg-bg-secondary rounded animate-pulse" />
        ))}
      </div>
    )
  }

  if (transactions.length === 0) {
    return (
      <div className="text-center py-12 text-ink-secondary operator-card">
        <p>No transactions found</p>
      </div>
    )
  }

  return (
    <div className="operator-card overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-panel-border">
            <th className="px-3 py-2 text-left text-xs font-semibold text-ink-tertiary">Type</th>
            <th className="px-3 py-2 text-left text-xs font-semibold text-ink-tertiary">Description</th>
            <th className="px-3 py-2 text-right text-xs font-semibold text-ink-tertiary">Amount</th>
            <th className="px-3 py-2 text-left text-xs font-semibold text-ink-tertiary">Balance</th>
            <th className="px-3 py-2 text-center text-xs font-semibold text-ink-tertiary">Status</th>
            <th className="px-3 py-2 text-left text-xs font-semibold text-ink-tertiary">Date</th>
            <th className="px-3 py-2 text-center text-xs font-semibold text-ink-tertiary">Action</th>
          </tr>
        </thead>
        <tbody>
          {transactions.map((txn) => {
            const typeConfig = TYPE_CONFIG[txn.type]
            const statusConfig = STATUS_CONFIG[txn.status]

            return (
              <tr key={txn.id} className="border-b border-panel-border hover:bg-bg-secondary transition-colors">
                <td className="px-3 py-2">
                  <span className={`text-lg ${typeConfig.color}`}>{typeConfig.icon}</span>
                </td>
                <td className="px-3 py-2">
                  <p className="text-ink-primary font-medium">{typeConfig.label}</p>
                  {txn.description && (
                    <p className="text-xs text-ink-secondary mt-1">{txn.description}</p>
                  )}
                  {txn.reference && (
                    <p className="text-xs text-ink-tertiary">{txn.reference}</p>
                  )}
                </td>
                <td className="px-3 py-2 text-right">
                  <p className={`font-semibold ${txn.amount > 0 ? 'text-status-healthy' : 'text-status-offline'}`}>
                    {txn.amount > 0 ? '+' : ''}{txn.amount.toLocaleString()}
                  </p>
                </td>
                <td className="px-3 py-2">
                  <div className="space-y-1">
                    <p className="text-xs text-ink-tertiary">
                      {txn.balanceBefore.toLocaleString()} → {txn.balanceAfter.toLocaleString()}
                    </p>
                  </div>
                </td>
                <td className="px-3 py-2 text-center">
                  <span className={`px-2 py-1 rounded text-xs font-medium ${statusConfig.bg} ${statusConfig.color}`}>
                    {txn.status}
                  </span>
                </td>
                <td className="px-3 py-2">
                  <p className="text-xs text-ink-secondary">
                    {formatDateTime(txn.createdAt)}
                  </p>
                </td>
                <td className="px-3 py-2 text-center">
                  {txn.status === 'completed' && txn.type === 'purchase' && onRefundClick && (
                    <button
                      onClick={() => onRefundClick(txn)}
                      className="text-xs text-accent hover:underline font-medium"
                    >
                      Refund
                    </button>
                  )}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
