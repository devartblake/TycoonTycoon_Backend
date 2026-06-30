/**
 * Dead-letter messages tab
 */

import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { useDeadLetterMessages, useRetryDeadLetter } from '../hooks/useNotifications'
import { formatDate } from '@/lib/utils'

export function DeadLetterTab() {
  const [filterChannel, setFilterChannel] = useState<string>('')
  const { data: messages = [], isLoading } = useDeadLetterMessages()
  const retryMessage = useRetryDeadLetter()

  const filteredMessages = filterChannel
    ? messages.filter((m) => m.channel === filterChannel)
    : messages

  const handleRetry = (messageId: string) => {
    retryMessage.mutate(messageId)
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold text-ink-primary">Failed Messages</h3>
        <select
          value={filterChannel}
          onChange={(e) => setFilterChannel(e.target.value)}
          className="px-3 py-2 border border-panel-border rounded focus-ring text-sm"
        >
          <option value="">All Channels</option>
          <option value="email">Email</option>
          <option value="push">Push</option>
          <option value="sms">SMS</option>
        </select>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="h-20 bg-bg-secondary rounded animate-pulse" />
          ))}
        </div>
      ) : filteredMessages.length === 0 ? (
        <div className="text-center py-12 text-ink-secondary">
          <p>No failed messages</p>
        </div>
      ) : (
        <div className="space-y-2">
          {filteredMessages.map((message) => (
            <div
              key={message.id}
              className="p-4 bg-bg-secondary border border-panel-border rounded space-y-2"
            >
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <h4 className="font-medium text-ink-primary">{message.templateName}</h4>
                  <p className="text-sm text-ink-secondary mt-1">
                    {message.channel} → {message.recipient}
                  </p>
                  <p className="text-xs text-status-offline mt-1">{message.error}</p>
                  <p className="text-xs text-ink-tertiary mt-2">
                    {message.attemptCount} attempt(s) • Last tried {formatDate(message.lastAttemptAt)}
                  </p>
                </div>

                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => handleRetry(message.id)}
                  disabled={retryMessage.isPending}
                >
                  Retry
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
