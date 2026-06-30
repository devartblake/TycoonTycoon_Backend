/**
 * Notification channels tab
 */

// import React from 'react'
import { useNotificationChannels, useUpdateChannel } from '../hooks/useNotifications'
import { formatDate } from '@/lib/utils'

export function ChannelsTab() {
  const { data: channels = [], isLoading } = useNotificationChannels()
  const updateChannel = useUpdateChannel()

  const handleToggle = (channelId: string, currentEnabled: boolean) => {
    updateChannel.mutate({
      channelId,
      enabled: !currentEnabled,
    })
  }

  if (isLoading) {
    return (
      <div className="space-y-3">
        {[...Array(3)].map((_, i) => (
          <div key={i} className="h-20 bg-bg-secondary rounded animate-pulse" />
        ))}
      </div>
    )
  }

  if (channels.length === 0) {
    return (
      <div className="text-center py-12 text-ink-secondary">
        <p>No channels configured</p>
      </div>
    )
  }

  return (
    <div className="space-y-3">
      {channels.map((channel) => (
        <div key={channel.id} className="p-4 bg-bg-secondary border border-panel-border rounded">
          <div className="flex items-center justify-between">
            <div className="flex-1">
              <h4 className="font-medium text-ink-primary capitalize">{channel.type}</h4>
              <p className="text-sm text-ink-secondary mt-1">{channel.name}</p>
              <p className="text-xs text-ink-tertiary mt-2">
                Created {formatDate(channel.createdAt)}
              </p>
            </div>

            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={channel.enabled}
                onChange={() => handleToggle(channel.id, channel.enabled)}
                disabled={updateChannel.isPending}
                className="cursor-pointer"
              />
              <span className="text-sm font-medium text-ink-primary">
                {channel.enabled ? 'Enabled' : 'Disabled'}
              </span>
            </label>
          </div>
        </div>
      ))}
    </div>
  )
}
