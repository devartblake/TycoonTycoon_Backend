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

// GET /admin/moderation/logs (ModerationLogItemDto). newStatus follows the
// backend ModerationStatus enum: 1=Normal 2=Suspected 3=Restricted 4=Banned.
export type ModerationLogStatus = 'normal' | 'suspected' | 'restricted' | 'banned'

export interface ModerationLog {
  id: string
  playerId: string
  newStatus: ModerationLogStatus
  reason: string
  notes?: string | null
  setByAdmin: string
  createdAt: string
  expiresAt?: string | null
  relatedFlagId?: string | null
}

export interface ModerationLogListResponse {
  items: ModerationLog[]
  total: number
  offset: number
  limit: number
}

export interface ModerationLogFilter {
  playerId?: string
  status?: ModerationLogStatus
}
