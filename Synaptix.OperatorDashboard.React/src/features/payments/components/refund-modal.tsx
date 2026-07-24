/**
 * Refund modal — issues a controlled refund for a captured payment attempt.
 * A blank amount performs a FULL refund, which also reverses the granted
 * entitlements on the backend.
 */

import { useState } from 'react'
import { Button } from '@/components/ui/button'
import type { PaymentAttempt } from '../types'

interface RefundModalProps {
  attempt: PaymentAttempt | null
  isOpen: boolean
  isLoading: boolean
  onClose: () => void
  onSubmit: (reason: string, amount?: number) => Promise<void>
}

export function RefundModal({ attempt, isOpen, isLoading, onClose, onSubmit }: RefundModalProps) {
  const [reason, setReason] = useState('')
  const [amount, setAmount] = useState('')
  const [error, setError] = useState<string | null>(null)

  if (!isOpen || !attempt) return null

  const parsedAmount = amount.trim() === '' ? undefined : Number(amount)
  const isFullRefund = parsedAmount === undefined || parsedAmount >= attempt.expectedAmount

  const handleSubmit = async () => {
    setError(null)
    if (!reason.trim()) {
      setError('A reason is required.')
      return
    }
    if (parsedAmount !== undefined && (Number.isNaN(parsedAmount) || parsedAmount <= 0)) {
      setError('Amount must be a positive number, or blank for a full refund.')
      return
    }
    try {
      await onSubmit(reason.trim(), parsedAmount)
      onClose()
      setReason('')
      setAmount('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Refund failed.')
    }
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="operator-card max-w-md w-full mx-4 space-y-4">
        <h2 className="text-lg font-semibold text-ink-primary">Refund payment</h2>

        <div className="p-3 bg-bg-secondary rounded space-y-1 text-sm">
          <div className="flex justify-between">
            <span className="text-ink-tertiary">SKU</span>
            <span className="font-mono text-ink-primary">{attempt.sku} ×{attempt.quantity}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-ink-tertiary">Charged</span>
            <span className="font-semibold text-accent">
              {attempt.expectedAmount.toFixed(2)} {attempt.currency}
            </span>
          </div>
          <div className="flex justify-between">
            <span className="text-ink-tertiary">Provider</span>
            <span className="uppercase text-ink-secondary">{attempt.provider}</span>
          </div>
        </div>

        <div>
          <label htmlFor="refund-reason" className="block text-sm font-medium text-ink-primary mb-1">
            Reason
          </label>
          <input
            id="refund-reason"
            type="text"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            className="w-full px-3 py-2 border border-panel-border rounded focus-ring"
            placeholder="e.g. Duplicate charge, customer request"
          />
        </div>

        <div>
          <label htmlFor="refund-amount" className="block text-sm font-medium text-ink-primary mb-1">
            Amount ({attempt.currency}) — blank for full refund
          </label>
          <input
            id="refund-amount"
            type="number"
            step="0.01"
            min="0"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            className="w-full px-3 py-2 border border-panel-border rounded focus-ring"
            placeholder={attempt.expectedAmount.toFixed(2)}
          />
          <p className="text-xs text-ink-secondary mt-2">
            {isFullRefund ? (
              <span className="text-status-warning font-medium">
                Full refund — granted entitlements will be reversed.
              </span>
            ) : (
              <>Partial refund — entitlements are kept.</>
            )}
          </p>
        </div>

        {error && (
          <div className="p-3 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm">
            {error}
          </div>
        )}

        <div className="flex gap-2 pt-4 border-t border-panel-border">
          <Button onClick={handleSubmit} disabled={isLoading} className="flex-1">
            {isLoading ? 'Refunding...' : 'Confirm refund'}
          </Button>
          <Button variant="secondary" onClick={onClose} disabled={isLoading} className="flex-1">
            Cancel
          </Button>
        </div>
      </div>
    </div>
  )
}
