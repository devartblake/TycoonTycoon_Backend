/**
 * Economy API — Django admin_economy_client + AdminEconomyEndpoints.
 *
 *   POST  /admin/economy/transactions
 *   GET   /admin/economy/history/{playerId}
 *   GET   /admin/economy/players/{id}          (player summary)
 *   GET   /admin/economy/stats
 *   GET   /admin/economy/balance               (game-balance policy)
 *   PATCH /admin/economy/balance
 *   POST  /admin/economy/simulate
 *   POST  /admin/economy/rollback              body: { eventId, reason }
 *   GET   /admin/player-lookup/search
 *
 * Transaction.id is backend EventId (rollback key).
 */

import { apiGet, apiPost, apiPatch } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type { PlayerEconomy, Transaction, TransactionListResponse, TransactionFilter, BalanceAdjustment, EconomyStats } from './types'

// CurrencyType enum (Xp=1, Coins=2, Diamonds=3) — numeric is the safe wire form.
const CURRENCY_COINS = 2

interface BackendEconomyLine {
  currency: number
  delta: number
}

interface BackendTxnListItem {
  eventId: string
  kind: string
  lines: BackendEconomyLine[]
  createdAtUtc: string
}

interface BackendEconomyHistory {
  playerId: string
  page: number
  pageSize: number
  total: number
  items: BackendTxnListItem[]
}

interface BackendTxnResult {
  eventId: string
  playerId: string
  status: number | string
  balanceXp: number
  balanceCoins: number
  balanceDiamonds: number
}

function offsetToPage(offset: number, limit: number): number {
  return Math.floor(offset / Math.max(1, limit)) + 1
}

function lineTotal(lines: BackendEconomyLine[]): number {
  // Prefer the Coins line; fall back to the summed delta.
  const coins = lines.find((l) => l.currency === CURRENCY_COINS)
  if (coins) return coins.delta
  return lines.reduce((sum, l) => sum + l.delta, 0)
}

function toTransaction(item: BackendTxnListItem): Transaction {
  const amount = lineTotal(item.lines)
  return {
    id: item.eventId,
    playerId: '',
    type: amount >= 0 ? 'earn' : 'penalty',
    amount,
    balanceBefore: 0,
    balanceAfter: 0,
    description: item.kind,
    reference: item.eventId,
    status: 'completed',
    createdAt: item.createdAtUtc,
  }
}

export async function getPlayerEconomy(playerId: string): Promise<PlayerEconomy> {
  if (getMockMode()) return mockApi.mockGetPlayerEconomy(playerId)
  return apiGet<PlayerEconomy>(`/admin/economy/players/${playerId}`)
}

export async function getPlayerTransactions(playerId: string, filters?: TransactionFilter, offset: number = 0, limit: number = 50): Promise<TransactionListResponse> {
  if (getMockMode()) return mockApi.mockGetPlayerTransactions(playerId, filters, offset, limit)
  const res = await apiGet<BackendEconomyHistory>(
    `/admin/economy/history/${playerId}?page=${offsetToPage(offset, limit)}&pageSize=${limit}`
  )
  let items = res.items.map((i) => ({ ...toTransaction(i), playerId }))
  if (filters?.type) items = items.filter((t) => t.type === filters.type)
  return { items, total: res.total, offset, limit }
}

export async function adjustBalance(adjustment: BalanceAdjustment): Promise<{ success: boolean; newBalance: number }> {
  if (getMockMode()) return mockApi.mockAdjustBalance(adjustment)
  // Post a single-line Coins ledger transaction (idempotent by EventId).
  const res = await apiPost<BackendTxnResult>('/admin/economy/transactions', {
    eventId: crypto.randomUUID(),
    playerId: adjustment.playerId,
    kind: 'admin-adjustment',
    lines: [{ currency: CURRENCY_COINS, delta: adjustment.amount }],
    note: adjustment.adminNote ? `${adjustment.reason} — ${adjustment.adminNote}` : adjustment.reason,
  })
  const applied = res.status === 1 || res.status === 'Applied'
  return { success: applied, newBalance: res.balanceCoins }
}

export async function issueRefund(playerId: string, transactionId: string, reason: string, _note?: string): Promise<{ success: boolean; refundAmount: number }> {
  if (getMockMode()) return mockApi.mockIssueRefund(playerId, transactionId, reason, _note)
  // Backend AdminRollbackEconomyRequest: EventId + Reason (not Django's transactionId field name)
  void playerId
  void _note
  await apiPost('/admin/economy/rollback', {
    eventId: transactionId,
    reason: reason?.trim() || 'Operator refund',
  })
  return { success: true, refundAmount: 0 }
}

export async function getEconomyStats(): Promise<EconomyStats> {
  if (getMockMode()) return mockApi.mockGetEconomyStats()
  return apiGet<EconomyStats>('/admin/economy/stats')
}

/** Django get_economy_balance — game balance policy config */
export async function getGameBalanceConfig(): Promise<unknown> {
  if (getMockMode()) return {}
  return apiGet('/admin/economy/balance')
}

/** Django update_economy_balance */
export async function updateGameBalanceConfig(payload: Record<string, unknown>): Promise<unknown> {
  if (getMockMode()) return payload
  return apiPatch('/admin/economy/balance', payload)
}

/** Django simulate_economy */
export async function simulateEconomy(payload: Record<string, unknown>): Promise<unknown> {
  if (getMockMode()) return { simulated: true }
  return apiPost('/admin/economy/simulate', payload)
}

interface BackendPlayerSearchItem {
  playerId: string
  email?: string | null
  username?: string | null
  coinsBalance: number
}

export async function searchPlayers(query: string, limit: number = 20): Promise<Array<{ playerId: string; email: string; handle: string; currentBalance: number }>> {
  if (getMockMode()) return mockApi.mockSearchPlayers(query, limit)
  const res = await apiGet<{ items: BackendPlayerSearchItem[]; total: number }>(
    `/admin/player-lookup/search?query=${encodeURIComponent(query)}&limit=${limit}`
  )
  return res.items.map((i) => ({
    playerId: i.playerId,
    email: i.email ?? '',
    handle: i.username ?? '',
    currentBalance: i.coinsBalance,
  }))
}
