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
