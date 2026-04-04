import { apiClient } from '../apiClient'
import type {
  ApplySeasonPointsRequest,
  ApplySeasonPointsResult,
  SeasonPointHistory,
} from '../types/admin'

function toQuery(params: Record<string, unknown>): string {
  const parts: string[] = []

  for (const [key, val] of Object.entries(params)) {
    if (val !== undefined && val !== null && val !== '')
      parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(val))}`)
  }

  return parts.join('&')
}

export const seasonPointsService = {
  history(playerId: string, params: { page?: number; pageSize?: number } = {}) {
    const qs = toQuery(params as Record<string, unknown>)

    return apiClient.get<SeasonPointHistory>(`/admin/season-points/history/${playerId}${qs ? `?${qs}` : ''}`)
  },

  applyPoints(req: ApplySeasonPointsRequest) {
    return apiClient.post<ApplySeasonPointsResult>('/admin/season-points/transactions', req)
  },
}
