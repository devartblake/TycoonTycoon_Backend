/**
 * Player profile header with status and quick stats
 */

import type { PlayerProfile } from '../types'

interface PlayerHeaderProps {
  profile: PlayerProfile
  isLoading: boolean
}

const STATUS_CONFIG = {
  active: { color: 'bg-status-healthy/10 text-status-healthy border-status-healthy', label: 'Active' },
  suspended: { color: 'bg-status-degraded/10 text-status-degraded border-status-degraded', label: 'Suspended' },
  banned: { color: 'bg-status-offline/10 text-status-offline border-status-offline', label: 'Banned' },
  inactive: { color: 'bg-ink-tertiary/10 text-ink-tertiary border-ink-tertiary', label: 'Inactive' },
}

export function PlayerHeader({ profile, isLoading }: PlayerHeaderProps) {
  if (isLoading) {
    return (
      <div className="operator-card space-y-4">
        <div className="h-12 bg-bg-secondary rounded animate-pulse" />
        <div className="grid grid-cols-2 md:grid-cols-5 gap-3">
          {[...Array(5)].map((_, i) => (
            <div key={i} className="h-16 bg-bg-secondary rounded animate-pulse" />
          ))}
        </div>
      </div>
    )
  }

  const config = STATUS_CONFIG[profile.status]
  const trustScoreColor = profile.trustScore >= 80 ? 'text-status-healthy' : profile.trustScore >= 50 ? 'text-status-degraded' : 'text-status-offline'

  return (
    <div className="operator-card space-y-6">
      {/* Title and Status */}
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-ink-primary">{profile.handle}</h1>
          <p className="text-sm text-ink-secondary mt-1">{profile.email}</p>
        </div>
        <span className={`px-4 py-2 rounded-lg border-2 font-semibold text-sm ${config.color}`}>
          {config.label}
        </span>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-4 pt-4 border-t border-panel-border">
        <div>
          <p className="text-xs text-ink-tertiary">Account Created</p>
          <p className="text-sm font-medium text-ink-primary mt-1">
            {new Date(profile.createdAt).toLocaleDateString()}
          </p>
        </div>
        <div>
          <p className="text-xs text-ink-tertiary">Last Active</p>
          <p className="text-sm font-medium text-ink-primary mt-1">
            {profile.lastActiveAt ? new Date(profile.lastActiveAt).toLocaleDateString() : 'Never'}
          </p>
        </div>
        <div>
          <p className="text-xs text-ink-tertiary">Flags</p>
          <p className={`text-lg font-bold mt-1 ${profile.flagCount > 0 ? 'text-status-offline' : 'text-status-healthy'}`}>
            {profile.flagCount}
          </p>
        </div>
        <div>
          <p className="text-xs text-ink-tertiary">Playtime</p>
          <p className="text-sm font-medium text-ink-primary mt-1">{profile.playtimeHours}h</p>
        </div>
        <div>
          <p className="text-xs text-ink-tertiary">Trust Score</p>
          <p className={`text-lg font-bold mt-1 ${trustScoreColor}`}>
            {Math.round(profile.trustScore)}%
          </p>
        </div>
      </div>

      {/* Economy Stats */}
      <div className="grid grid-cols-2 md:grid-cols-3 gap-4 pt-4 border-t border-panel-border">
        <div>
          <p className="text-xs text-ink-tertiary">Balance</p>
          <p className="text-lg font-bold text-accent mt-1">
            {profile.accountBalance.toLocaleString()}
          </p>
        </div>
        <div>
          <p className="text-xs text-ink-tertiary">Total Spent</p>
          <p className="text-sm font-medium text-ink-secondary mt-1">
            {profile.totalSpent.toLocaleString()}
          </p>
        </div>
        <div>
          <p className="text-xs text-ink-tertiary">Win Rate</p>
          <p className="text-sm font-medium text-ink-primary mt-1">
            {profile.winRate.toFixed(1)}%
          </p>
        </div>
      </div>
    </div>
  )
}
