/**
 * Users API client — aligned to backend AdminUsersEndpoints + Django admin_users_client.
 *
 *   GET    /admin/users
 *   GET    /admin/users/{userId}
 *   PATCH  /admin/users/{userId}
 *   POST   /admin/users/{userId}/ban
 *   POST   /admin/users/{userId}/unban
 *   GET    /admin/users/{userId}/activity
 *   GET    /admin/player-lookup/resolve?query=
 *
 * Backend list uses page/pageSize (not offset/limit) and field `q` for search.
 */

import { apiGet, apiPost, apiPatch } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type {
  UsersListResponse,
  UserFilters,
  SavedView,
  UserDetail,
  UserActivityResponse,
  User,
} from './types'

interface BackendUserListItemDto {
  id: string
  username: string
  email: string
  status: string
  role: string
  ageGroup: string
  createdAt: string
  lastActive: string | null
  totalGamesPlayed: number
  totalPoints: number
  winRate: number
  isVerified: boolean
  isBanned: boolean
}

interface BackendUsersListResponse {
  items: BackendUserListItemDto[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

interface BackendUserDetailDto extends BackendUserListItemDto {
  metadata?: Record<string, unknown>
}

function mapStatus(dto: { status: string; isBanned: boolean }): User['status'] {
  if (dto.isBanned) return 'banned'
  const s = dto.status?.toLowerCase()
  if (s === 'active' || s === 'suspended' || s === 'banned' || s === 'inactive') return s
  return 'active'
}

function toUser(dto: BackendUserListItemDto): User {
  return {
    id: dto.id,
    email: dto.email,
    handle: dto.username,
    status: mapStatus(dto),
    createdAt: dto.createdAt,
    lastActiveAt: dto.lastActive,
    flaggedCount: 0,
  }
}

function toUserDetail(dto: BackendUserDetailDto): UserDetail {
  return {
    ...toUser(dto),
    username: dto.username,
    role: dto.role,
    ageGroup: dto.ageGroup,
    totalGamesPlayed: dto.totalGamesPlayed,
    totalPoints: dto.totalPoints,
    winRate: dto.winRate,
    isVerified: dto.isVerified,
    isBanned: dto.isBanned,
  }
}

export async function getUsers(filters: UserFilters): Promise<UsersListResponse> {
  if (getMockMode()) return mockApi.mockGetUsers(filters)

  const limit = filters.limit || 50
  const offset = filters.offset || 0
  const page = Math.floor(offset / Math.max(1, limit)) + 1

  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(limit),
  })

  // Backend search is `q` (email/username), not `email`
  if (filters.email) params.set('q', filters.email)

  if (filters.status === 'banned') {
    params.set('isBanned', 'true')
  } else if (filters.status) {
    params.set('status', filters.status)
  }

  // `flagged` is not a backend list filter — ignored (client may post-filter later)

  const res = await apiGet<BackendUsersListResponse>(`/admin/users?${params}`)
  return {
    items: res.items.map(toUser),
    total: res.totalItems,
    offset,
    limit,
  }
}

export async function getUserDetail(userId: string): Promise<UserDetail> {
  if (getMockMode()) return mockApi.mockGetUserDetail(userId)
  const dto = await apiGet<BackendUserDetailDto>(`/admin/users/${userId}`)
  return toUserDetail(dto)
}

export async function getUserActivity(
  userId: string,
  page: number = 1,
  pageSize: number = 20
): Promise<UserActivityResponse> {
  if (getMockMode()) return mockApi.mockGetUserActivity(userId, page, pageSize)
  return apiGet(`/admin/users/${userId}/activity?page=${page}&pageSize=${pageSize}`)
}

export async function banUser(userId: string, reason?: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockBanUser(userId, reason)
  // AdminBanUserRequest requires Reason
  await apiPost(`/admin/users/${userId}/ban`, {
    reason: reason?.trim() || 'Banned by operator',
    until: null,
  })
  return { success: true }
}

export async function unbanUser(userId: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockUnbanUser(userId)
  await apiPost(`/admin/users/${userId}/unban`, {})
  return { success: true }
}

// No separate suspend on user accounts — use ban (same as prior React note / Django-era UX).
export async function suspendUser(userId: string, reason?: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockBanUser(userId, reason)
  return banUser(userId, reason || 'Suspended by operator')
}

export async function unsuspendUser(userId: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockUnbanUser(userId)
  return unbanUser(userId)
}

export async function bulkBanUsers(
  userIds: string[],
  reason?: string
): Promise<{ success: boolean; affected: number }> {
  if (getMockMode()) return { success: true, affected: userIds.length }
  const results = await Promise.allSettled(userIds.map((id) => banUser(id, reason)))
  return { success: true, affected: results.filter((r) => r.status === 'fulfilled').length }
}

export async function bulkUnbanUsers(userIds: string[]): Promise<{ success: boolean; affected: number }> {
  if (getMockMode()) return { success: true, affected: userIds.length }
  const results = await Promise.allSettled(userIds.map((id) => unbanUser(id)))
  return { success: true, affected: results.filter((r) => r.status === 'fulfilled').length }
}

/** PATCH /admin/users/{id} — Django update_admin_user parity */
export async function updateUser(
  userId: string,
  payload: { username?: string; role?: string; isVerified?: boolean }
): Promise<{ id: string }> {
  if (getMockMode()) return { id: userId }
  return apiPatch(`/admin/users/${userId}`, payload)
}

/** GET /admin/player-lookup/resolve — Django resolve_player_lookup parity */
export async function resolvePlayerLookup(query: string): Promise<unknown> {
  if (getMockMode()) return { query, results: [] }
  return apiGet(`/admin/player-lookup/resolve?query=${encodeURIComponent(query)}`)
}

// Saved Views were Django-local (operator DB). Not on .NET admin API.
export async function getSavedViews(): Promise<SavedView[]> {
  if (getMockMode()) return mockApi.mockGetSavedViews()
  return []
}

export async function createSavedView(_name: string, _filters: UserFilters): Promise<SavedView> {
  if (getMockMode()) return mockApi.mockCreateSavedView(_name, _filters)
  void _filters
  throw new Error('Saved views are not yet available on the React dashboard API. Use filters in the URL for now.')
}

export async function deleteSavedView(viewId: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockDeleteSavedView(viewId)
  void viewId
  throw new Error('Saved views are not yet available on the React dashboard API.')
}
