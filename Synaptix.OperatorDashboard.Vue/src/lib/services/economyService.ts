import { apiClient } from '../apiClient'
import type {
  CreateEconomyTxnRequest,
  EconomyHistory,
  EconomyTxnResult,
} from '../types/admin'

function toQuery(params: Record<string, unknown>): string {
  const parts: string[] = []

  for (const [key, val] of Object.entries(params)) {
    if (val !== undefined && val !== null && val !== '')
      parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(val))}`)
  }

  return parts.join('&')
}

export const economyService = {
  history(playerId: string, params: { page?: number; pageSize?: number } = {}) {
    const qs = toQuery(params as Record<string, unknown>)

    return apiClient.get<EconomyHistory>(`/admin/economy/history/${playerId}${qs ? `?${qs}` : ''}`)
  },

  createTransaction(req: CreateEconomyTxnRequest) {
    return apiClient.post<EconomyTxnResult>('/admin/economy/transactions', req)
  },

  rollback(eventId: string, reason: string) {
    return apiClient.post<EconomyTxnResult>('/admin/economy/rollback', { eventId, reason })
  },
}
