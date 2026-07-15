/**
 * Player Economy & Transactions
 * - /economy/player → balances + adjust
 * - /economy/player-transactions → ledger / refunds focus
 */

import { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonTable } from '@/components/shared/skeletons'
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

type EconomyMode = 'balances' | 'transactions'

function modeFromPath(pathname: string): EconomyMode {
  return pathname.includes('player-transactions') ? 'transactions' : 'balances'
}

export default function PlayerEconomyPage() {
  usePermission('economy:read')
  const location = useLocation()
  const mode = modeFromPath(location.pathname)

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
    await adjustBalanceMutation.mutateAsync({
      playerId: selectedPlayerId,
      amount,
      reason,
      adminNote: note,
    })
    setSuccessMessage('Balance adjusted successfully')
    setTimeout(() => setSuccessMessage(null), 3000)
  }

  const handleIssueRefund = async (transaction: Transaction) => {
    if (!selectedPlayerId) return
    const reason = 'Player requested refund'
    await issueRefundMutation.mutateAsync({
      playerId: selectedPlayerId,
      transactionId: transaction.id,
      reason,
    })
    setSuccessMessage('Refund processed successfully')
    setTimeout(() => setSuccessMessage(null), 3000)
  }

  const economy = economyQuery.data
  const transactions = transactionsQuery.data?.items || []
  const isLoading = economyQuery.isLoading || transactionsQuery.isLoading

  const title = mode === 'balances' ? 'Player Economy' : 'Player Transactions'
  const subtitle =
    mode === 'balances'
      ? 'Lookup balances, wallet status, and perform balance adjustments'
      : 'Inspect ledger history and process refunds for a player'

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-6">
        <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold text-ink-primary">{title}</h1>
            <p className="mt-2 text-ink-secondary">{subtitle}</p>
          </div>
          <div className="flex rounded border border-panel-border overflow-hidden text-sm">
            <Link
              to="/economy/player"
              className={`px-4 py-2 ${
                mode === 'balances' ? 'bg-accent text-white' : 'bg-bg-secondary text-ink-secondary hover:bg-bg-tertiary'
              }`}
            >
              Balances
            </Link>
            <Link
              to="/economy/player-transactions"
              className={`px-4 py-2 border-l border-panel-border ${
                mode === 'transactions'
                  ? 'bg-accent text-white'
                  : 'bg-bg-secondary text-ink-secondary hover:bg-bg-tertiary'
              }`}
            >
              Transactions
            </Link>
          </div>
        </div>

        {successMessage && (
          <div className="p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm">
            ✓ {successMessage}
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="space-y-4">
            <PlayerSearch onSelectPlayer={handleSelectPlayer} />

            {mode === 'balances' && selectedPlayerId && economy && (
              <button
                onClick={() => setShowAdjustModal(true)}
                className="w-full px-4 py-2 bg-accent text-white rounded font-medium hover:bg-accent-dark transition-smooth disabled:opacity-50"
                disabled={adjustBalanceMutation.isPending}
              >
                {adjustBalanceMutation.isPending ? 'Adjusting...' : 'Adjust Balance'}
              </button>
            )}

            {mode === 'transactions' && selectedPlayerId && (
              <div className="operator-card text-xs text-ink-secondary space-y-1">
                <p className="font-medium text-ink-primary">Ledger tools</p>
                <p>Select a row’s refund action to reverse a charge. Filters coming soon.</p>
              </div>
            )}
          </div>

          {selectedPlayerId && mode === 'balances' && (
            <div className="lg:col-span-2">
              <EconomySummary economy={economy} isLoading={isLoading} />
            </div>
          )}

          {selectedPlayerId && mode === 'transactions' && economy && (
            <div className="lg:col-span-2 operator-card">
              <p className="text-xs text-ink-tertiary">Selected player</p>
              <p className="text-lg font-semibold text-ink-primary mt-1 font-mono text-sm">
                {selectedPlayerId}
              </p>
              <p className="text-sm text-ink-secondary mt-2">
                Current balance:{' '}
                <span className="font-semibold text-accent">
                  {economy.currentBalance?.toLocaleString?.() ?? economy.currentBalance}
                </span>
              </p>
            </div>
          )}
        </div>

        {/* Balances mode: compact recent activity */}
        {selectedPlayerId && mode === 'balances' && (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold text-ink-primary">Recent activity</h2>
              <Link
                to="/economy/player-transactions"
                className="text-sm text-accent hover:underline"
              >
                Full ledger →
              </Link>
            </div>
            {transactionsQuery.isLoading ? (
              <SkeletonTable rows={5} columns={5} />
            ) : transactions.length > 0 ? (
              <TransactionsTable
                transactions={transactions.slice(0, 8)}
                isLoading={false}
                onRefundClick={handleIssueRefund}
              />
            ) : (
              <EmptyState
                title="No transactions found"
                description="This player has no transaction history yet"
                icon="💰"
              />
            )}
          </div>
        )}

        {/* Transactions mode: full paginated ledger */}
        {selectedPlayerId && mode === 'transactions' && (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-ink-primary">Transaction ledger</h2>
            {transactionsQuery.isLoading ? (
              <SkeletonTable rows={8} columns={5} />
            ) : transactions.length > 0 ? (
              <TransactionsTable
                transactions={transactions}
                isLoading={false}
                onRefundClick={handleIssueRefund}
              />
            ) : (
              <EmptyState
                title="No transactions found"
                description="This player has no transaction history yet"
                icon="💰"
              />
            )}

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
                  Page {Math.floor(offset / limit) + 1} of{' '}
                  {Math.ceil(transactionsQuery.data.total / limit)}
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

        {!selectedPlayerId && (
          <EmptyState
            title="No player selected"
            description={
              mode === 'balances'
                ? 'Search for a player to view balances and adjust currency'
                : 'Search for a player to open their full transaction ledger'
            }
            icon="🔍"
          />
        )}
      </div>
    </ErrorBoundary>
  )
}
