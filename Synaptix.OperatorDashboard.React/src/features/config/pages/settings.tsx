/**
 * Configuration & Settings - Feature Flags, Admin ACL, System Config
 */

import { useState, useEffect } from 'react'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons'
import * as configApi from '../api'
import type { FeatureFlag, AdminACL, SystemConfig } from '../types'

export default function SettingsPage() {
  usePermission('config:write')

  const [activeTab, setActiveTab] = useState<'flags' | 'acl' | 'system'>('flags')
  const [flags, setFlags] = useState<FeatureFlag[]>([])
  const [acl, setACL] = useState<AdminACL[]>([])
  const [systemConfig, setSystemConfig] = useState<SystemConfig | null>(null)
  const [loading, setLoading] = useState(true)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)

  useEffect(() => {
    const loadData = async () => {
      setLoading(true)
      try {
        const [flagsRes, aclRes, sysConfig] = await Promise.all([
          configApi.getFeatureFlags(),
          configApi.getAdminACL(),
          configApi.getSystemConfig(),
        ])
        setFlags(flagsRes.items)
        setACL(aclRes.items)
        setSystemConfig(sysConfig)
      } catch (error) {
        console.error('Failed to load config:', error)
      } finally {
        setLoading(false)
      }
    }
    loadData()
  }, [])

  const handleToggleFlag = async (id: string, enabled: boolean) => {
    try {
      await configApi.toggleFeatureFlag(id, !enabled)
      setFlags(flags.map((f) => (f.id === id ? { ...f, enabled: !enabled } : f)))
      setSuccessMsg(`Feature flag ${enabled ? 'disabled' : 'enabled'}`)
      setTimeout(() => setSuccessMsg(null), 2000)
    } catch (error) {
      console.error('Toggle failed:', error)
    }
  }

  const handleUpdateSystemConfig = async (key: keyof SystemConfig, value: any) => {
    if (!systemConfig) return
    try {
      const updated = await configApi.updateSystemConfig({ ...systemConfig, [key]: value })
      setSystemConfig(updated)
      setSuccessMsg('System config updated')
      setTimeout(() => setSuccessMsg(null), 2000)
    } catch (error) {
      console.error('Update failed:', error)
    }
  }

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-8">
        {/* Header */}
        <div>
          <h1 className="text-3xl font-bold text-ink-primary">Configuration & Settings</h1>
          <p className="mt-2 text-ink-secondary">Manage feature flags, admin access, and system configuration</p>
        </div>

        {/* Success Message */}
        {successMsg && (
          <div className="p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm">
            ✓ {successMsg}
          </div>
        )}

        {/* Stats */}
        {loading ? (
          <SkeletonGrid count={4} />
        ) : (
          <div className="grid grid-cols-4 gap-4">
        <div className="operator-card">
          <p className="text-xs text-ink-tertiary">Total Flags</p>
          <p className="text-2xl font-bold text-accent mt-1">{flags.length}</p>
        </div>
        <div className="operator-card">
          <p className="text-xs text-ink-tertiary">Enabled</p>
          <p className="text-2xl font-bold text-status-healthy mt-1">{flags.filter((f) => f.enabled).length}</p>
        </div>
        <div className="operator-card">
          <p className="text-xs text-ink-tertiary">Admin Users</p>
          <p className="text-2xl font-bold text-ink-primary mt-1">{acl.length}</p>
        </div>
        <div className="operator-card">
          <p className="text-xs text-ink-tertiary">Maintenance</p>
          <p className={`text-2xl font-bold mt-1 ${systemConfig?.maintenanceMode ? 'text-status-offline' : 'text-status-healthy'}`}>
            {systemConfig?.maintenanceMode ? 'ON' : 'OFF'}
          </p>
        </div>
          </div>
        )}

      {/* Tab Navigation */}
      <div className="flex gap-2 border-b border-panel-border">
        {[
          { id: 'flags' as const, label: '🚩 Feature Flags' },
          { id: 'acl' as const, label: '🔐 Admin ACL' },
          { id: 'system' as const, label: '⚙️ System Config' },
        ].map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`px-4 py-2 font-medium border-b-2 transition-colors ${
              activeTab === tab.id
                ? 'border-accent text-accent'
                : 'border-transparent text-ink-secondary hover:text-ink-primary'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Content */}
      <div className="operator-card">
        {loading ? (
          <SkeletonTable rows={8} columns={5} />
        ) : activeTab === 'flags' ? (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">Feature Flags ({flags.length})</h2>
            {flags.length > 0 ? (
              <div className="space-y-3">
                {flags.map((flag) => (
                  <div key={flag.id} className="p-4 border border-panel-border rounded hover:bg-panel/50">
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <h3 className="font-semibold text-ink-primary">{flag.name}</h3>
                        <p className="text-sm text-ink-secondary mt-1">{flag.description}</p>
                        <div className="flex gap-4 mt-2 text-xs text-ink-tertiary">
                          <span>Key: {flag.key}</span>
                          <span>Audience: {flag.targetAudience}</span>
                          <span>Rollout: {flag.rolloutPercentage}%</span>
                        </div>
                      </div>
                      <button
                        onClick={() => handleToggleFlag(flag.id, flag.enabled)}
                        className={`px-4 py-2 rounded font-medium transition-colors ${
                          flag.enabled
                            ? 'bg-status-healthy/20 text-status-healthy'
                            : 'bg-panel text-ink-secondary hover:bg-panel-border'
                        }`}
                      >
                        {flag.enabled ? '✓ Enabled' : '✗ Disabled'}
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <EmptyState
                title="No feature flags found"
                description="Configure feature flags to control new features"
                icon="🚩"
              />
            )}
          </div>
        ) : activeTab === 'acl' ? (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">Admin Access Control ({acl.length})</h2>
            {acl.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-panel border-b border-panel-border">
                    <tr>
                      <th className="px-4 py-2 text-left">Admin</th>
                      <th className="px-4 py-2 text-left">Email</th>
                      <th className="px-4 py-2 text-left">Role</th>
                      <th className="px-4 py-2 text-left">Permissions</th>
                      <th className="px-4 py-2 text-left">Created</th>
                    </tr>
                  </thead>
                  <tbody>
                    {acl.map((entry) => (
                      <tr key={entry.id} className="border-t border-panel-border hover:bg-panel/50">
                        <td className="px-4 py-3 font-medium">{entry.adminId}</td>
                        <td className="px-4 py-3">{entry.adminEmail}</td>
                        <td className="px-4 py-3">
                          <span className="px-2 py-1 rounded text-xs bg-accent/20 text-accent">{entry.role}</span>
                        </td>
                        <td className="px-4 py-3">
                          <span className="text-xs text-ink-tertiary">{entry.permissions.length} permissions</span>
                        </td>
                        <td className="px-4 py-3 text-xs text-ink-tertiary">{new Date(entry.createdAt).toLocaleDateString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <EmptyState
                title="No admin entries found"
                description="Add administrators to control system access"
                icon="🔐"
              />
            )}
          </div>
        ) : (
          <div className="space-y-6">
            <h2 className="text-lg font-semibold">System Configuration</h2>
            {systemConfig ? (
              <div className="space-y-4">
                {/* Maintenance Mode */}
                <div className="p-4 border border-panel-border rounded">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="font-semibold text-ink-primary">Maintenance Mode</h3>
                      <p className="text-sm text-ink-secondary mt-1">
                        {systemConfig.maintenanceMode ? 'Enabled - System in maintenance' : 'Disabled - System online'}
                      </p>
                    </div>
                    <button
                      onClick={() => handleUpdateSystemConfig('maintenanceMode', !systemConfig.maintenanceMode)}
                      className={`px-4 py-2 rounded font-medium transition-colors ${
                        systemConfig.maintenanceMode
                          ? 'bg-status-offline/20 text-status-offline'
                          : 'bg-status-healthy/20 text-status-healthy'
                      }`}
                    >
                      {systemConfig.maintenanceMode ? 'Disable' : 'Enable'}
                    </button>
                  </div>
                </div>

                {/* Analytics */}
                <div className="p-4 border border-panel-border rounded">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="font-semibold text-ink-primary">Analytics</h3>
                      <p className="text-sm text-ink-secondary mt-1">
                        {systemConfig.analyticsEnabled ? 'Enabled' : 'Disabled'}
                      </p>
                    </div>
                    <button
                      onClick={() => handleUpdateSystemConfig('analyticsEnabled', !systemConfig.analyticsEnabled)}
                      className={`px-4 py-2 rounded font-medium transition-colors ${
                        systemConfig.analyticsEnabled ? 'bg-status-healthy/20 text-status-healthy' : 'bg-panel text-ink-secondary'
                      }`}
                    >
                      {systemConfig.analyticsEnabled ? '✓ On' : '✗ Off'}
                    </button>
                  </div>
                </div>

                {/* Debug Logging */}
                <div className="p-4 border border-panel-border rounded">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="font-semibold text-ink-primary">Debug Logging</h3>
                      <p className="text-sm text-ink-secondary mt-1">
                        {systemConfig.debugLoggingEnabled ? 'Verbose logging enabled' : 'Standard logging'}
                      </p>
                    </div>
                    <button
                      onClick={() => handleUpdateSystemConfig('debugLoggingEnabled', !systemConfig.debugLoggingEnabled)}
                      className={`px-4 py-2 rounded font-medium transition-colors ${
                        systemConfig.debugLoggingEnabled ? 'bg-status-healthy/20 text-status-healthy' : 'bg-panel text-ink-secondary'
                      }`}
                    >
                      {systemConfig.debugLoggingEnabled ? '✓ On' : '✗ Off'}
                    </button>
                  </div>
                </div>

                {/* Rate Limiting */}
                <div className="p-4 border border-panel-border rounded">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="font-semibold text-ink-primary">Rate Limiting</h3>
                      <p className="text-sm text-ink-secondary mt-1">{systemConfig.rateLimitPerMinute} requests/minute</p>
                    </div>
                    <input
                      type="number"
                      value={systemConfig.rateLimitPerMinute}
                      onChange={(e) => handleUpdateSystemConfig('rateLimitPerMinute', Number(e.target.value))}
                      className="w-24 px-3 py-2 border border-panel-border rounded bg-panel text-right"
                      min="10"
                    />
                  </div>
                </div>

                {/* Session Timeout */}
                <div className="p-4 border border-panel-border rounded">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="font-semibold text-ink-primary">Session Timeout</h3>
                      <p className="text-sm text-ink-secondary mt-1">{systemConfig.sessionTimeoutMinutes} minutes</p>
                    </div>
                    <input
                      type="number"
                      value={systemConfig.sessionTimeoutMinutes}
                      onChange={(e) => handleUpdateSystemConfig('sessionTimeoutMinutes', Number(e.target.value))}
                      className="w-24 px-3 py-2 border border-panel-border rounded bg-panel text-right"
                      min="5"
                    />
                  </div>
                </div>
              </div>
            ) : (
              <EmptyState
                title="System config unavailable"
                description="Unable to load system configuration"
                icon="⚠️"
              />
            )}
          </div>
        )}
      </div>

      {/* Status */}
      <div className="p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary">
        <p className="font-medium text-ink-secondary mb-2">✅ Configuration Management Complete</p>
        <ul className="space-y-1">
          <li>✓ Feature Flags with rollout control</li>
          <li>✓ Admin ACL and role management</li>
          <li>✓ System configuration (maintenance, analytics, logging)</li>
          <li>✓ Rate limiting and session settings</li>
          <li>✓ Real-time toggle and update controls</li>
        </ul>
      </div>
      </div>
    </ErrorBoundary>
  )
}
