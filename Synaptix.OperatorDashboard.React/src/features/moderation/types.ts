/**
 * Moderation feature types
 */

export interface PlayerProfile {
  id: string
  email: string
  handle: string
  status: 'active' | 'suspended' | 'banned' | 'inactive'
  createdAt: string
  lastActiveAt: string | null
  flagCount: number
  accountBalance: number
  totalSpent: number
  playtimeHours: number
  winRate: number
  trustScore: number // 0-100
}

export interface ModerationAction {
  id: string
  playerId: string
  adminEmail: string
  action: 'ban' | 'unban' | 'suspend' | 'unsuspend' | 'warn' | 'note'
  reason: string
  notes?: string
  duration?: number // milliseconds for suspensions
  expiresAt?: string
  status: 'active' | 'expired' | 'revoked'
  createdAt: string
}

export interface PlayerActivity {
  id: string
  playerId: string
  type: 'login' | 'game_played' | 'purchase' | 'violation' | 'appeal' | 'action'
  description: string
  metadata?: Record<string, any>
  timestamp: string
}

export interface PlayerModeration {
  profile: PlayerProfile
  actions: ModerationAction[]
  activity: PlayerActivity[]
  stats: {
    totalWarnings: number
    totalBans: number
    lastAction?: ModerationAction
  }
}
