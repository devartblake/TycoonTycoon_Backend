/**
 * Player moderation action panel
 */

import { useState } from 'react'
import { Button } from '@/components/ui/button'
import type { PlayerProfile } from '../types'

interface ActionPanelProps {
  profile: PlayerProfile
  onBan: (reason: string, notes?: string) => Promise<void>
  onUnban: (reason: string) => Promise<void>
  onSuspend: (durationHours: number, reason: string, notes?: string) => Promise<void>
  onUnsuspend: (reason: string) => Promise<void>
  onWarn: (reason: string, notes?: string) => Promise<void>
  isLoading: boolean
}

const ACTION_REASONS = [
  'Cheating detected',
  'Abusive behavior',
  'Account sharing',
  'Payment fraud',
  'Terms violation',
  'Appeal granted',
  'Manual review',
]

const SUSPENSION_DURATIONS = [
  { hours: 1, label: '1 hour' },
  { hours: 24, label: '1 day' },
  { hours: 168, label: '1 week' },
  { hours: 720, label: '30 days' },
]

export function ActionPanel({ profile, onBan, onUnban, onSuspend, onUnsuspend, onWarn, isLoading }: ActionPanelProps) {
  const [activeAction, setActiveAction] = useState<string | null>(null)
  const [reason, setReason] = useState('')
  const [notes, setNotes] = useState('')
  const [suspensionDuration, setSuspensionDuration] = useState(24)
  const [error, setError] = useState<string | null>(null)

  const handleSubmitAction = async (action: () => Promise<void>) => {
    setError(null)
    try {
      await action()
      setActiveAction(null)
      setReason('')
      setNotes('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed')
    }
  }

  return (
    <div className="operator-card space-y-4">
      <h3 className="font-semibold text-ink-primary">Quick Actions</h3>

      {error && (
        <div className="p-3 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm">
          {error}
        </div>
      )}

      {/* Action Buttons Grid */}
      <div className="grid grid-cols-2 gap-2">
        {profile.status !== 'banned' && (
          <Button
            variant={activeAction === 'ban' ? 'default' : 'outline'}
            size="sm"
            onClick={() => setActiveAction(activeAction === 'ban' ? null : 'ban')}
            className="text-xs"
          >
            Ban Player
          </Button>
        )}

        {profile.status === 'banned' && (
          <Button
            variant={activeAction === 'unban' ? 'default' : 'outline'}
            size="sm"
            onClick={() => setActiveAction(activeAction === 'unban' ? null : 'unban')}
            className="text-xs"
          >
            Unban
          </Button>
        )}

        {profile.status !== 'suspended' && (
          <Button
            variant={activeAction === 'suspend' ? 'default' : 'outline'}
            size="sm"
            onClick={() => setActiveAction(activeAction === 'suspend' ? null : 'suspend')}
            className="text-xs"
          >
            Suspend
          </Button>
        )}

        {profile.status === 'suspended' && (
          <Button
            variant={activeAction === 'unsuspend' ? 'default' : 'outline'}
            size="sm"
            onClick={() => setActiveAction(activeAction === 'unsuspend' ? null : 'unsuspend')}
            className="text-xs"
          >
            Unsuspend
          </Button>
        )}

        <Button
          variant={activeAction === 'warn' ? 'default' : 'outline'}
          size="sm"
          onClick={() => setActiveAction(activeAction === 'warn' ? null : 'warn')}
          className="text-xs"
        >
          Warn
        </Button>
      </div>

      {/* Action Forms */}
      {(activeAction === 'ban' || activeAction === 'suspend' || activeAction === 'warn' || activeAction === 'unban' || activeAction === 'unsuspend') && (
        <div className="space-y-3 p-3 bg-bg-secondary rounded border border-panel-border">
          {/* Reason Dropdown */}
          <div>
            <label className="block text-xs font-medium text-ink-tertiary mb-1">Reason</label>
            <select
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              className="w-full px-2 py-2 text-sm border border-panel-border rounded focus-ring bg-bg-primary"
            >
              <option value="">Select a reason...</option>
              {ACTION_REASONS.map((r) => (
                <option key={r} value={r}>
                  {r}
                </option>
              ))}
            </select>
          </div>

          {/* Suspension Duration */}
          {activeAction === 'suspend' && (
            <div>
              <label className="block text-xs font-medium text-ink-tertiary mb-1">Duration</label>
              <div className="grid grid-cols-2 gap-2">
                {SUSPENSION_DURATIONS.map((d) => (
                  <button
                    key={d.hours}
                    onClick={() => setSuspensionDuration(d.hours)}
                    className={`px-2 py-1 text-xs rounded border transition-colors ${
                      suspensionDuration === d.hours
                        ? 'bg-accent text-white border-accent'
                        : 'border-panel-border hover:bg-bg-tertiary'
                    }`}
                  >
                    {d.label}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Notes */}
          {(activeAction === 'ban' || activeAction === 'suspend' || activeAction === 'warn') && (
            <div>
              <label htmlFor="notes" className="block text-xs font-medium text-ink-tertiary mb-1">
                Notes (optional)
              </label>
              <textarea
                id="notes"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                className="w-full px-2 py-2 text-xs border border-panel-border rounded focus-ring h-20 resize-none"
                placeholder="Internal notes..."
              />
            </div>
          )}

          {/* Submit */}
          <div className="flex gap-2 pt-2">
            <Button
              size="sm"
              onClick={() => {
                if (!reason) {
                  setError('Please select a reason')
                  return
                }
                if (activeAction === 'ban') {
                  handleSubmitAction(() => onBan(reason, notes))
                } else if (activeAction === 'unban') {
                  handleSubmitAction(() => onUnban(reason))
                } else if (activeAction === 'suspend') {
                  handleSubmitAction(() => onSuspend(suspensionDuration, reason, notes))
                } else if (activeAction === 'unsuspend') {
                  handleSubmitAction(() => onUnsuspend(reason))
                } else if (activeAction === 'warn') {
                  handleSubmitAction(() => onWarn(reason, notes))
                }
              }}
              disabled={isLoading}
              className="flex-1 text-xs"
            >
              {isLoading ? 'Submitting...' : 'Confirm'}
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => {
                setActiveAction(null)
                setReason('')
                setNotes('')
              }}
              className="flex-1 text-xs"
            >
              Cancel
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
