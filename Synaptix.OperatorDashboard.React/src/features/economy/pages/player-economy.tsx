/**
 * Player Economy & Transactions page
 */

import { useState } from 'react'
import { usePermission } from '@/hooks/use-permission'
import { PlayerSearch } from '../components/player-search'
import { EconomySummary } from '../components/economy-summary'
import { TransactionsTable } from '../components/transactions-table'
import { BalanceAdjustmentModal } from '../components/balance-adjustment-modal'
import {
  usePlayerEconomy,
  usePlayerTransactions,
  useAdjustBalance,
  useIssueRefund,
} from '../hooks/useEconomy'
import type { Transaction } from '../types'

export default function PlayerEconomyPage() {
  usePermission('economy:read')

  const [selectedPlayerId, setSelectedPlayerId] = useState<string | null>(null)
  const [offset, setOffset] = useState(0)
  const [showAdjustModal, setShowAdjustModal] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const limit = 50

  const economyQuery = usePlayerEconomy(selectedPlayerId || '')
  const transactionsQuery = usePlayerTransactions(selectedPlayerId || '', undefined, offset, limit)
  const adjustBalanceMutation = useAdjustBalance()
  const issueRefundMutation = useIssueRefund()

  const handleSelectPlayer = (playerId: string) => {
    setSelectedPlayerId(playerId)
    setOffset(0)
  }

  const handleAdjustBalance = async (amount: number, reason: string, note?: string) => {
    if (!selectedPlayerId) return
    await adjustBalanceMutation.mutateAsync({ playerId: selectedPlayerId, amount, reason, adminNote: note })
    setSuccessMessage('Balance adjusted successfully')
    setTimeout(() => setSuccessMessage(null), 3000)
  }

  const handleIssueRefund = async (transaction: Transaction) => {
    if (!selectedPlayerId) return
    // Open refund confirmation modal (simplified to direct call here)
    const reason = 'Player requested refund'
    await issueRefundMutation.mutateAsync({ playerId: selectedPlayerId, transactionId: transaction.id, reason })
    setSuccessMessage('Refund processed successfully')
    setTimeout(() => setSuccessMessage(null), 3000)
  }

  const economy = economyQuery.data
  const transactions = transactionsQuery.data?.items || []
  const isLoading = economyQuery.isLoading || transactionsQuery.isLoading

  return (
    <div className="operator-container space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-ink-primary">Player Economy</h1>
        <p className="mt-2 text-ink-secondary">Manage player balance and view transaction history</p>
      </div>

      {/* Success Message */}
      {successMessage && (
        <div className="p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm">
          ✓ {successMessage}
        </div>
      )}

      {/* Search and Summary */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left: Search and Actions */}
        <div className="space-y-4">
          <PlayerSearch onSelectPlayer={handleSelectPlayer} />

          {selectedPlayerId && economy && (
            <button
              onClick={() => setShowAdjustModal(true)}
              className="w-full px-4 py-2 bg-accent text-white rounded font-medium hover:bg-accent-dark transition-smooth disabled:opacity-50"
              disabled={adjustBalanceMutation.isPending}
            >
              {adjustBalanceMutation.isPending ? 'Adjusting...' : 'Adjust Balance'}
            </button>
          )}
        </div>

        {/* Right: Economy Summary */}
        {selectedPlayerId && (
          <div className="lg:col-span-2">
            <EconomySummary economy={economy} isLoading={isLoading} />
          </div>
        )}
      </div>

      {/* Transactions */}
      {selectedPlayerId && (
        <div className="space-y-4">
          <div>
            <h2 className="text-lg font-semibold text-ink-primary mb-4">Transaction History</h2>
            <TransactionsTable
              transactions={transactions}
              isLoading={transactionsQuery.isLoading}
              onRefundClick={handleIssueRefund}
            />
          </div>

          {/* Pagination */}
          {transactionsQuery.data && transactionsQuery.data.total > limit && (
            <div className="flex justify-between items-center">
              <button
                onClick={() => setOffset(Math.max(0, offset - limit))}
                disabled={offset === 0}
                className="px-4 py-2 bg-bg-secondary border border-panel-border rounded text-sm hover:bg-bg-tertiary disabled:opacity-50 disabled:cursor-not-allowed"
              >
                ← Previous
              </button>
              <p className="text-sm text-ink-secondary">
                Page {Math.floor(offset / limit) + 1} of {Math.ceil(transactionsQuery.data.total / limit)}
              </p>
              <button
                onClick={() => setOffset(offset + limit)}
                disabled={offset + limit >= transactionsQuery.data.total}
                className="px-4 py-2 bg-bg-secondary border border-panel-border rounded text-sm hover:bg-bg-tertiary disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Next →
              </button>
            </div>
          )}
        </div>
      )}

      {/* Balance Adjustment Modal */}
      {selectedPlayerId && economy && (
        <BalanceAdjustmentModal
          playerId={selectedPlayerId}
          currentBalance={economy.currentBalance}
          isOpen={showAdjustModal}
          onClose={() => setShowAdjustModal(false)}
          onSubmit={handleAdjustBalance}
          isLoading={adjustBalanceMutation.isPending}
        />
      )}

      {/* Empty State */}
      {!selectedPlayerId && (
        <div className="text-center py-12 text-ink-secondary">
          <p>Select a player to view their economy information</p>
        </div>
      )}
    </div>
  )
}
