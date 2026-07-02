/**
 * Economy API client
 */

import { apiGet, apiPost } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type { PlayerEconomy, TransactionListResponse, TransactionFilter, BalanceAdjustment, EconomyStats } from './types'

export async function getPlayerEconomy(playerId: string): Promise<PlayerEconomy> {
  if (getMockMode()) return mockApi.mockGetPlayerEconomy(playerId)
  return apiGet(`/admin/economy/players/${playerId}`)
}

export async function getPlayerTransactions(playerId: string, filters?: TransactionFilter, offset: number = 0, limit: number = 50): Promise<TransactionListResponse> {
  if (getMockMode()) return mockApi.mockGetPlayerTransactions(playerId, filters, offset, limit)
  const params = new URLSearchParams({
    offset: offset.toString(),
    limit: limit.toString(),
    ...Object.fromEntries(
      Object.entries(filters || {}).filter(([, v]) => v != null).map(([k, v]) => [k, String(v)])
    ),
  })
  return apiGet(`/admin/economy/players/${playerId}/transactions?${params}`)
}

export async function adjustBalance(adjustment: BalanceAdjustment): Promise<{ success: boolean; newBalance: number }> {
  if (getMockMode()) return mockApi.mockAdjustBalance(adjustment)
  return apiPost(`/admin/economy/players/${adjustment.playerId}/adjust-balance`, {
    amount: adjustment.amount,
    reason: adjustment.reason,
    adminNote: adjustment.adminNote,
  })
}

export async function issueRefund(playerId: string, transactionId: string, reason: string, note?: string): Promise<{ success: boolean; refundAmount: number }> {
  if (getMockMode()) return mockApi.mockIssueRefund(playerId, transactionId, reason, note)
  return apiPost(`/admin/economy/players/${playerId}/refund`, {
    transactionId,
    reason,
    note,
  })
}

export async function getEconomyStats(): Promise<EconomyStats> {
  if (getMockMode()) return mockApi.mockGetEconomyStats()
  return apiGet('/admin/economy/stats')
}

export async function searchPlayers(query: string, limit: number = 20): Promise<Array<{ playerId: string; email: string; handle: string; currentBalance: number }>> {
  if (getMockMode()) return mockApi.mockSearchPlayers(query, limit)
  return apiGet(`/admin/economy/players/search?q=${encodeURIComponent(query)}&limit=${limit}`)
}
