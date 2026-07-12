/**
 * Users feature types
 */

export interface User {
  id: string
  email: string
  status: 'active' | 'suspended' | 'banned' | 'inactive'
  createdAt: string
  lastActiveAt: string | null
  flaggedCount: number
  handle?: string
}

export interface UsersListResponse {
  items: User[]
  total: number
  offset: number
  limit: number
}

export interface UserFilters {
  email?: string
  status?: User['status']
  flagged?: boolean
  offset?: number
  limit?: number
}

export interface SavedView {
  id: string
  name: string
  filters: UserFilters
  createdAt: string
  updatedAt: string
}

export interface BulkActionPayload {
  userIds: string[]
  action: 'ban' | 'unban' | 'suspend' | 'unsuspend'
  reason?: string
}

// Rich detail shape from GET /admin/users/{id} (AdminUserDetailDto)
export interface UserDetail extends User {
  username: string
  role: string
  ageGroup: string
  totalGamesPlayed: number
  totalPoints: number
  winRate: number
  isVerified: boolean
  isBanned: boolean
}

// GET /admin/users/{id}/activity (AdminUserActivityResponse)
export interface UserActivityItem {
  id: string
  type: string
  description: string
  createdAt: string
  metadata?: Record<string, unknown>
}

export interface UserActivityResponse {
  items: UserActivityItem[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}
