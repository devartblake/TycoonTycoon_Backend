/**
 * Question review verdict panel
 */

import { useState } from 'react'
import { Button } from '@/components/ui/button'

interface ReviewPanelProps {
  onApprove: (reason?: string, notes?: string) => Promise<void>
  onReject: (reason: string, notes?: string) => Promise<void>
  isLoading: boolean
}

const REJECTION_REASONS = [
  'Incorrect answer key',
  'Ambiguous question',
  'Offensive content',
  'Duplicate question',
  'Poor grammar/spelling',
  'Unclear wording',
  'Inappropriate difficulty',
  'Missing explanation',
]

export function ReviewPanel({ onApprove, onReject, isLoading }: ReviewPanelProps) {
  const [verdict, setVerdict] = useState<'approve' | 'reject' | null>(null)
  const [reason, setReason] = useState('')
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async () => {
    setError(null)

    if (!verdict) {
      setError('Please select a verdict')
      return
    }

    if (verdict === 'reject' && !reason) {
      setError('Please select a rejection reason')
      return
    }

    try {
      if (verdict === 'approve') {
        await onApprove(reason, notes)
      } else {
        await onReject(reason, notes)
      }
      // Reset form after success
      setVerdict(null)
      setReason('')
      setNotes('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit review')
    }
  }

  return (
    <div className="operator-card space-y-4">
      <h3 className="font-semibold text-ink-primary">Review Question</h3>

      {error && (
        <div className="p-3 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm">
          {error}
        </div>
      )}

      {/* Verdict Buttons */}
      <div className="grid grid-cols-2 gap-2">
        <button
          onClick={() => {
            setVerdict('approve')
            setReason('')
          }}
          className={`px-3 py-2 rounded border-2 font-medium text-sm transition-colors ${
            verdict === 'approve'
              ? 'bg-status-healthy/10 border-status-healthy text-status-healthy'
              : 'border-panel-border hover:bg-bg-secondary text-ink-secondary'
          }`}
        >
          ✓ Approve
        </button>
        <button
          onClick={() => setVerdict('reject')}
          className={`px-3 py-2 rounded border-2 font-medium text-sm transition-colors ${
            verdict === 'reject'
              ? 'bg-status-offline/10 border-status-offline text-status-offline'
              : 'border-panel-border hover:bg-bg-secondary text-ink-secondary'
          }`}
        >
          ✕ Reject
        </button>
      </div>

      {/* Rejection Reason (only for reject) */}
      {verdict === 'reject' && (
        <div>
          <label className="block text-xs font-medium text-ink-tertiary mb-2">Rejection Reason</label>
          <div className="space-y-2">
            {REJECTION_REASONS.map((r) => (
              <label key={r} className="flex items-center gap-2 p-2 rounded hover:bg-bg-secondary cursor-pointer">
                <input
                  type="radio"
                  name="reason"
                  value={r}
                  checked={reason === r}
                  onChange={(e) => setReason(e.target.value)}
                  className="cursor-pointer"
                />
                <span className="text-xs text-ink-secondary">{r}</span>
              </label>
            ))}
          </div>
        </div>
      )}

      {/* Notes */}
      <div>
        <label htmlFor="notes" className="block text-xs font-medium text-ink-tertiary mb-1">
          Reviewer Notes (optional)
        </label>
        <textarea
          id="notes"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          className="w-full px-3 py-2 border border-panel-border rounded focus-ring text-sm h-20 resize-none"
          placeholder="Add notes about your review..."
        />
      </div>

      {/* Submit */}
      <Button
        onClick={handleSubmit}
        disabled={isLoading || !verdict}
        className="w-full"
      >
        {isLoading ? 'Submitting...' : 'Submit Review & Continue'}
      </Button>
    </div>
  )
}
