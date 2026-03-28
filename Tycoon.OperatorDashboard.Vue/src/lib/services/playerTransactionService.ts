import { apiClient } from '../apiClient'
import type {
  DisputePlayerTransactionRequest,
  PlayerTransactionDetail,
  PlayerTransactionHistory,
  ReversePlayerTransactionRequest,
} from '../types/admin'

function toQuery(params: Record<string, unknown>): string {
  const parts: string[] = []

  for (const [key, val] of Object.entries(params)) {
    if (val !== undefined && val !== null && val !== '')
      parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(val))}`)
  }

  return parts.join('&')
}

export const playerTransactionService = {
  history(params: { playerId?: string; correlatedEventId?: string; page?: number; pageSize?: number } = {}) {
    const qs = toQuery(params as Record<string, unknown>)

    return apiClient.get<PlayerTransactionHistory>(`/admin/player-transactions/history${qs ? `?${qs}` : ''}`)
  },

  detail(id: string) {
    return apiClient.get<PlayerTransactionDetail>(`/admin/player-transactions/${id}`)
  },

  dispute(req: DisputePlayerTransactionRequest) {
    return apiClient.post<unknown>('/admin/player-transactions/dispute', req)
  },

  reverse(req: ReversePlayerTransactionRequest) {
    return apiClient.post<unknown>('/admin/player-transactions/reverse', req)
  },
}
