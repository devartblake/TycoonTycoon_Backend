/**
 * Users API client
 */

import { apiGet, apiPost } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type { UsersListResponse, UserFilters, SavedView, UserDetail, UserActivityResponse } from './types'

export async function getUsers(filters: UserFilters): Promise<UsersListResponse> {
  if (getMockMode()) return mockApi.mockGetUsers(filters)

  const params = new URLSearchParams()
  if (filters.email) params.append('email', filters.email)
  if (filters.status) params.append('status', filters.status)
  if (filters.flagged !== undefined) params.append('flagged', String(filters.flagged))
  params.append('limit', String(filters.limit || 50))
  params.append('offset', String(filters.offset || 0))

  return apiGet(`/admin/users?${params.toString()}`)
}

// Backend AdminUserDetailDto — richer than the list's User shape.
interface BackendUserDetailDto {
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

function toUserDetail(dto: BackendUserDetailDto): UserDetail {
  const status = dto.status?.toLowerCase()
  return {
    id: dto.id,
    email: dto.email,
    handle: dto.username,
    status: status === 'active' || status === 'suspended' || status === 'banned' || status === 'inactive'
      ? status
      : dto.isBanned ? 'banned' : 'active',
    createdAt: dto.createdAt,
    lastActiveAt: dto.lastActive,
    flaggedCount: 0, // not exposed by the detail DTO
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

export async function getUserDetail(userId: string): Promise<UserDetail> {
  if (getMockMode()) return mockApi.mockGetUserDetail(userId)
  const dto = await apiGet<BackendUserDetailDto>(`/admin/users/${userId}`)
  return toUserDetail(dto)
}

export async function getUserActivity(userId: string, page: number = 1, pageSize: number = 20): Promise<UserActivityResponse> {
  if (getMockMode()) return mockApi.mockGetUserActivity(userId, page, pageSize)
  return apiGet(`/admin/users/${userId}/activity?page=${page}&pageSize=${pageSize}`)
}

export async function banUser(userId: string, reason?: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockBanUser(userId, reason)
  return apiPost(`/admin/users/${userId}/ban`, { reason })
}

export async function unbanUser(userId: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockUnbanUser(userId)
  return apiPost(`/admin/users/${userId}/unban`, {})
}

// The backend has no separate suspend state; a suspension is applied as a ban
// (see #425). Mapped so the operator action stays functional.
export async function suspendUser(userId: string, reason?: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockBanUser(userId, reason)
  return apiPost(`/admin/users/${userId}/ban`, { reason })
}

export async function unsuspendUser(userId: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockUnbanUser(userId)
  return apiPost(`/admin/users/${userId}/unban`, {})
}

// The backend has no bulk endpoint; apply per-user ban/unban and aggregate.
export async function bulkBanUsers(userIds: string[], reason?: string): Promise<{ success: boolean; affected: number }> {
  if (getMockMode()) return { success: true, affected: userIds.length }
  const results = await Promise.allSettled(
    userIds.map((id) => apiPost(`/admin/users/${id}/ban`, { reason }))
  )
  return { success: true, affected: results.filter((r) => r.status === 'fulfilled').length }
}

export async function bulkUnbanUsers(userIds: string[]): Promise<{ success: boolean; affected: number }> {
  if (getMockMode()) return { success: true, affected: userIds.length }
  const results = await Promise.allSettled(
    userIds.map((id) => apiPost(`/admin/users/${id}/unban`, {}))
  )
  return { success: true, affected: results.filter((r) => r.status === 'fulfilled').length }
}

// Saved Views are owned by the Django operator dashboard (see #425); the .NET
// backend does not serve them. These are no-ops in the React app to avoid 404s.
export async function getSavedViews(): Promise<SavedView[]> {
  if (getMockMode()) return mockApi.mockGetSavedViews()
  return []
}

export async function createSavedView(_name: string, _filters: UserFilters): Promise<SavedView> {
  if (getMockMode()) return mockApi.mockCreateSavedView(_name, _filters)
  void _filters
  throw new Error('Saved views are managed in the Django operator dashboard.')
}

export async function deleteSavedView(viewId: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockDeleteSavedView(viewId)
  void viewId
  throw new Error('Saved views are managed in the Django operator dashboard.')
}
