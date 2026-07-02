/**
 * Balance adjustment modal dialog
 */

import { useState } from 'react'
import { Button } from '@/components/ui/button'

interface BalanceAdjustmentModalProps {
  playerId?: string
  currentBalance: number
  isOpen: boolean
  onClose: () => void
  onSubmit: (amount: number, reason: string, note?: string) => Promise<void>
  isLoading: boolean
}

const ADJUSTMENT_REASONS = [
  'Customer complaint resolution',
  'Event bonus',
  'Bug compensation',
  'Refund correction',
  'Account error fix',
  'Support adjustment',
]

export function BalanceAdjustmentModal({
  currentBalance,
  isOpen,
  onClose,
  onSubmit,
  isLoading,
}: BalanceAdjustmentModalProps) {
  const [amount, setAmount] = useState(0)
  const [reason, setReason] = useState('')
  const [note, setNote] = useState('')
  const [error, setError] = useState<string | null>(null)

  if (!isOpen) return null

  const newBalance = currentBalance + amount

  const handleSubmit = async () => {
    setError(null)

    if (!reason) {
      setError('Please select a reason')
      return
    }

    if (amount === 0) {
      setError('Please enter an amount')
      return
    }

    try {
      await onSubmit(amount, reason, note)
      onClose()
      setAmount(0)
      setReason('')
      setNote('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to adjust balance')
    }
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="operator-card max-w-md w-full mx-4 space-y-4">
        <h2 className="text-lg font-semibold text-ink-primary">Adjust Player Balance</h2>

        {/* Current Balance */}
        <div className="p-3 bg-bg-secondary rounded">
          <p className="text-xs text-ink-tertiary">Current Balance</p>
          <p className="text-2xl font-bold text-accent mt-1">{currentBalance.toLocaleString()}</p>
        </div>

        {/* Amount Input */}
        <div>
          <label htmlFor="amount" className="block text-sm font-medium text-ink-primary mb-1">
            Amount Change
          </label>
          <input
            id="amount"
            type="number"
            value={amount}
            onChange={(e) => setAmount(Number(e.target.value))}
            className="w-full px-3 py-2 border border-panel-border rounded focus-ring"
            placeholder="Positive or negative amount"
          />
          <p className="text-xs text-ink-secondary mt-2">
            New Balance: <span className={newBalance < 0 ? 'text-status-offline font-bold' : 'text-status-healthy font-bold'}>{newBalance.toLocaleString()}</span>
          </p>
        </div>

        {/* Reason */}
        <div>
          <label htmlFor="reason" className="block text-sm font-medium text-ink-primary mb-1">
            Reason
          </label>
          <select
            id="reason"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            className="w-full px-3 py-2 border border-panel-border rounded focus-ring bg-bg-primary"
          >
            <option value="">Select a reason...</option>
            {ADJUSTMENT_REASONS.map((r) => (
              <option key={r} value={r}>
                {r}
              </option>
            ))}
          </select>
        </div>

        {/* Note */}
        <div>
          <label htmlFor="note" className="block text-sm font-medium text-ink-primary mb-1">
            Internal Note (optional)
          </label>
          <textarea
            id="note"
            value={note}
            onChange={(e) => setNote(e.target.value)}
            className="w-full px-3 py-2 border border-panel-border rounded focus-ring h-20 resize-none text-sm"
            placeholder="Additional context for this adjustment..."
          />
        </div>

        {/* Error */}
        {error && (
          <div className="p-3 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm">
            {error}
          </div>
        )}

        {/* Actions */}
        <div className="flex gap-2 pt-4 border-t border-panel-border">
          <Button onClick={handleSubmit} disabled={isLoading} className="flex-1">
            {isLoading ? 'Adjusting...' : 'Confirm'}
          </Button>
          <Button onClick={onClose} variant="outline" className="flex-1" disabled={isLoading}>
            Cancel
          </Button>
        </div>
      </div>
    </div>
  )
}
