/**
 * Bulk actions bar for selected users
 */

import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { useBulkBanUsers, useBulkUnbanUsers } from '../hooks/useUsers'

interface BulkActionsBarProps {
  selectedIds: string[]
  onActionComplete: () => void
}

export function BulkActionsBar({ selectedIds, onActionComplete }: BulkActionsBarProps) {
  const [reason, setReason] = useState('')
  const [showReasonForm, setShowReasonForm] = useState(false)
  const bulkBan = useBulkBanUsers()
  const bulkUnban = useBulkUnbanUsers()

  if (selectedIds.length === 0) {
    return null
  }

  const handleBulkBan = async () => {
    if (confirm(`Ban ${selectedIds.length} user(s)?`)) {
      await bulkBan.mutateAsync({ userIds: selectedIds, reason: reason || undefined })
      setReason('')
      onActionComplete()
    }
  }

  const handleBulkUnban = async () => {
    if (confirm(`Unban ${selectedIds.length} user(s)?`)) {
      await bulkUnban.mutateAsync(selectedIds)
      onActionComplete()
    }
  }

  return (
    <div className="sticky bottom-0 bg-bg-secondary border-t border-panel-border p-4 space-y-3">
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium text-ink-primary">
          {selectedIds.length} user(s) selected
        </span>

        <div className="flex gap-2">
          <Button
            variant="destructive"
            size="sm"
            onClick={() => setShowReasonForm(!showReasonForm)}
            disabled={bulkBan.isPending}
          >
            🚫 Ban
          </Button>
          <Button
            variant="secondary"
            size="sm"
            onClick={handleBulkUnban}
            disabled={bulkUnban.isPending}
          >
            ✓ Unban
          </Button>
        </div>
      </div>

      {showReasonForm && (
        <div className="flex gap-2">
          <input
            type="text"
            placeholder="Reason (optional)..."
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            className="flex-1 px-3 py-2 border border-panel-border rounded focus-ring text-sm"
          />
          <Button
            variant="default"
            size="sm"
            onClick={handleBulkBan}
            disabled={bulkBan.isPending}
          >
            {bulkBan.isPending ? 'Banning...' : 'Confirm Ban'}
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              setShowReasonForm(false)
              setReason('')
            }}
          >
            Cancel
          </Button>
        </div>
      )}

      {bulkBan.isError && (
        <div className="text-xs text-status-offline">Error: {String(bulkBan.error)}</div>
      )}
      {bulkUnban.isError && (
        <div className="text-xs text-status-offline">Error: {String(bulkUnban.error)}</div>
      )}
    </div>
  )
}
