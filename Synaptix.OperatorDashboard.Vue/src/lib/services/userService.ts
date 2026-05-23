import { apiClient } from '../apiClient'
import type {
  AdminBanUserRequest,
  AdminBanUserResponse,
  AdminCreateUserRequest,
  AdminUpdateUserRequest,
  AdminUserActivityItem,
  AdminUserDetail,
  AdminUserListItem,
  AdminUsersListRequest,
  PaginatedResponse,
} from '../types/admin'

function toQuery(params: Record<string, unknown>): string {
  const parts: string[] = []

  for (const [key, val] of Object.entries(params)) {
    if (val !== undefined && val !== null && val !== '')
      parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(val))}`)
  }

  return parts.join('&')
}

export const userService = {
  list(params: AdminUsersListRequest = {}) {
    const qs = toQuery(params as Record<string, unknown>)

    return apiClient.get<PaginatedResponse<AdminUserListItem>>(`/admin/users${qs ? `?${qs}` : ''}`)
  },

  get(userId: string) {
    return apiClient.get<AdminUserDetail>(`/admin/users/${userId}`)
  },

  create(req: AdminCreateUserRequest) {
    return apiClient.post<{ id: string; createdAt: string }>('/admin/users', req)
  },

  update(userId: string, req: AdminUpdateUserRequest) {
    return apiClient.patch<{ id: string; updatedAt: string }>(`/admin/users/${userId}`, req)
  },

  ban(userId: string, req: AdminBanUserRequest) {
    return apiClient.post<AdminBanUserResponse>(`/admin/users/${userId}/ban`, req)
  },

  unban(userId: string) {
    return apiClient.post<{ id: string; isBanned: boolean }>(`/admin/users/${userId}/unban`)
  },

  delete(userId: string) {
    return apiClient.delete<void>(`/admin/users/${userId}`)
  },

  activity(userId: string, params: { page?: number; pageSize?: number; type?: string; from?: string; to?: string } = {}) {
    const qs = toQuery(params as Record<string, unknown>)

    return apiClient.get<PaginatedResponse<AdminUserActivityItem>>(`/admin/users/${userId}/activity${qs ? `?${qs}` : ''}`)
  },
}
