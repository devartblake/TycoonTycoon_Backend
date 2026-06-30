/**
 * Anti-cheat verdict submission form
 */

import { useState } from 'react'
import { Button } from '@/components/ui/button'
import type { VerdictPayload } from '../types'

interface VerdictFormProps {
  flagId: string
  onSubmit: (payload: VerdictPayload) => Promise<void>
  isLoading: boolean
}

const VERDICTS = [
  { value: 'innocent', label: '✓ Innocent', color: 'bg-status-healthy/10 text-status-healthy' },
  { value: 'suspicious', label: '? Suspicious', color: 'bg-status-degraded/10 text-status-degraded' },
  { value: 'confirmed', label: '✕ Confirmed Cheat', color: 'bg-status-offline/10 text-status-offline' },
] as const

export function VerdictForm({ flagId, onSubmit, isLoading }: VerdictFormProps) {
  const [verdict, setVerdict] = useState<'innocent' | 'suspicious' | 'confirmed' | null>(null)
  const [notes, setNotes] = useState('')
  const [submitError, setSubmitError] = useState<string | null>(null)

  const handleSubmit = async () => {
    if (!verdict) {
      setSubmitError('Please select a verdict')
      return
    }

    setSubmitError(null)

    try {
      await onSubmit({
        flagId,
        verdict,
        notes: notes || undefined,
      })
      // Reset form after success
      setVerdict(null)
      setNotes('')
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Failed to submit verdict')
    }
  }

  return (
    <div className="operator-card space-y-4">
      <h3 className="font-semibold text-ink-primary">Submit Verdict</h3>

      {/* Verdict Options */}
      <div className="space-y-2">
        {VERDICTS.map((v) => (
          <label key={v.value} className="flex items-center gap-3 p-3 border border-panel-border rounded cursor-pointer hover:bg-bg-secondary transition-colors" style={{ borderColor: verdict === v.value ? 'var(--accent)' : undefined }}>
            <input
              type="radio"
              name="verdict"
              value={v.value}
              checked={verdict === v.value}
              onChange={(e) => setVerdict(e.target.value as typeof verdict)}
              className="cursor-pointer"
            />
            <span className={`px-2 py-1 rounded text-xs font-medium ${v.color}`}>
              {v.label}
            </span>
          </label>
        ))}
      </div>

      {/* Notes */}
      <div>
        <label htmlFor="notes" className="block text-sm font-medium text-ink-primary mb-1">
          Reviewer Notes (optional)
        </label>
        <textarea
          id="notes"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          className="w-full px-3 py-2 border border-panel-border rounded focus-ring h-24"
          placeholder="Add notes about your verdict..."
        />
      </div>

      {/* Error */}
      {submitError && (
        <div className="p-3 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm">
          {submitError}
        </div>
      )}

      {/* Submit Button */}
      <Button
        onClick={handleSubmit}
        disabled={isLoading || !verdict}
        className="w-full"
      >
        {isLoading ? 'Submitting...' : 'Submit Verdict & Continue'}
      </Button>
    </div>
  )
}
