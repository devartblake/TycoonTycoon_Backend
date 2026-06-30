/**
 * Scheduled notifications tab
 */

import React, { useState } from 'react'
import { Button } from '@/components/ui/button'
import { useScheduledNotifications, useCreateSchedule, useCancelSchedule } from '../hooks/useNotifications'
import { formatDate } from '@/lib/utils'

export function ScheduleTab() {
  const [showCreateForm, setShowCreateForm] = useState(false)
  const [templateId, setTemplateId] = useState('')
  const [scheduledFor, setScheduledFor] = useState('')

  const { data: schedules = [], isLoading } = useScheduledNotifications()
  const createSchedule = useCreateSchedule()
  const cancelSchedule = useCancelSchedule()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!templateId || !scheduledFor) return

    await createSchedule.mutateAsync({
      templateId,
      scheduledFor,
    })

    setTemplateId('')
    setScheduledFor('')
    setShowCreateForm(false)
  }

  const handleCancel = (scheduleId: string) => {
    if (confirm('Cancel this scheduled notification?')) {
      cancelSchedule.mutate(scheduleId)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold text-ink-primary">Scheduled Sends</h3>
        <Button
          variant="default"
          size="sm"
          onClick={() => setShowCreateForm(!showCreateForm)}
        >
          {showCreateForm ? '✕ Cancel' : '+ Schedule Send'}
        </Button>
      </div>

      {/* Create form */}
      {showCreateForm && (
        <form onSubmit={handleSubmit} className="p-4 bg-bg-secondary border border-panel-border rounded space-y-4">
          <div>
            <label htmlFor="template-select" className="block text-sm font-medium text-ink-primary mb-1">
              Template
            </label>
            <input
              id="template-select"
              type="text"
              value={templateId}
              onChange={(e) => setTemplateId(e.target.value)}
              placeholder="Template ID or name"
              className="w-full px-3 py-2 border border-panel-border rounded focus-ring text-sm"
              required
            />
          </div>

          <div>
            <label htmlFor="schedule-time" className="block text-sm font-medium text-ink-primary mb-1">
              Scheduled For
            </label>
            <input
              id="schedule-time"
              type="datetime-local"
              value={scheduledFor}
              onChange={(e) => setScheduledFor(e.target.value)}
              className="w-full px-3 py-2 border border-panel-border rounded focus-ring text-sm"
              required
            />
          </div>

          <div className="flex gap-2 justify-end">
            <Button
              type="button"
              variant="ghost"
              onClick={() => setShowCreateForm(false)}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              variant="default"
              disabled={createSchedule.isPending || !templateId || !scheduledFor}
            >
              {createSchedule.isPending ? 'Creating...' : 'Schedule'}
            </Button>
          </div>
        </form>
      )}

      {/* List */}
      {isLoading ? (
        <div className="space-y-2">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="h-16 bg-bg-secondary rounded animate-pulse" />
          ))}
        </div>
      ) : schedules.length === 0 ? (
        <div className="text-center py-12 text-ink-secondary">
          <p>No scheduled notifications</p>
        </div>
      ) : (
        <div className="space-y-2">
          {schedules.map((schedule) => (
            <div
              key={schedule.id}
              className="p-4 bg-bg-secondary border border-panel-border rounded flex items-center justify-between"
            >
              <div>
                <h4 className="font-medium text-ink-primary">{schedule.templateName}</h4>
                <p className="text-sm text-ink-secondary mt-1">
                  Scheduled for {formatDate(schedule.scheduledFor)}
                </p>
                <p className="text-xs text-ink-tertiary mt-1">
                  {schedule.targetCount} target(s) • Status: {schedule.status}
                </p>
              </div>

              {schedule.status === 'pending' && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleCancel(schedule.id)}
                  disabled={cancelSchedule.isPending}
                >
                  Cancel
                </Button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
