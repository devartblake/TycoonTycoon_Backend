/**
 * Notifications Hub page
 */

import { useState } from 'react'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import { TemplatesTab } from '../components/templates-tab'
import { ChannelsTab } from '../components/channels-tab'
import { ScheduleTab } from '../components/schedule-tab'
import { DeadLetterTab } from '../components/dead-letter-tab'

type Tab = 'templates' | 'channels' | 'schedule' | 'dead-letter'

const TABS: { id: Tab; label: string; icon: string }[] = [
  { id: 'templates', label: 'Templates', icon: '📧' },
  { id: 'channels', label: 'Channels', icon: '📢' },
  { id: 'schedule', label: 'Schedule', icon: '📅' },
  { id: 'dead-letter', label: 'Failed', icon: '⚠️' },
]

export default function NotificationsHub() {
  usePermission('notifications:read')

  const [activeTab, setActiveTab] = useState<Tab>('templates')

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-ink-primary">Notifications Hub</h1>
        <p className="mt-2 text-ink-secondary">Manage notification templates, channels, and schedules</p>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 border-b border-panel-border">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`px-4 py-3 font-medium text-sm border-b-2 transition-colors ${
              activeTab === tab.id
                ? 'border-accent text-accent'
                : 'border-transparent text-ink-secondary hover:text-ink-primary'
            }`}
          >
            {tab.icon} {tab.label}
          </button>
        ))}
      </div>

      {/* Tab content */}
      <div>
        {activeTab === 'templates' && <TemplatesTab />}
        {activeTab === 'channels' && <ChannelsTab />}
        {activeTab === 'schedule' && <ScheduleTab />}
        {activeTab === 'dead-letter' && <DeadLetterTab />}
      </div>
      </div>
    </ErrorBoundary>
  )
}
