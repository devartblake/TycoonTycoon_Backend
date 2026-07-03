/**
 * Economy API client
 *
 * Reconciled to the real backend route surface under /admin/economy
 * (Synaptix.Backend.Api/Features/AdminEconomy) plus /admin/player-lookup. The
 * backend models a currency *ledger* (EventId-keyed multi-line transactions) and
 * game-balance policy — it does not expose a per-player economy summary or
 * aggregate currency stats. Functions keep their existing return types.
 *
 * Mapping notes (see #421):
 *   - Transaction.id is the backend EventId, so refunds roll back by that id.
 *   - Balance adjustments post a single-line Coins transaction.
 *   - getPlayerEconomy (summary) and getEconomyStats (aggregate) have NO backend
 *     source and throw a clear error until those endpoints are built.
 */

import { apiGet, apiPost } from '@/lib/api-client'
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

interface BackendPlayerLookup {
  playerId?: string
  userId?: string
  email?: string
  handle?: string
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

export async function getPlayerEconomy(_playerId: string): Promise<PlayerEconomy> {
  if (getMockMode()) return mockApi.mockGetPlayerEconomy(_playerId)
  // No backend per-player economy summary (balances/totals). Needs a backend
  // endpoint (see #421) before this view can be populated from real data.
  void _playerId
  throw new Error('Per-player economy summary is not yet available from the backend (#421).')
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
  // Transaction.id is the backend EventId, so a refund rolls that event back.
  void _note
  await apiPost('/admin/economy/rollback', { eventId: transactionId, reason })
  return { success: true, refundAmount: 0 }
}

export async function getEconomyStats(): Promise<EconomyStats> {
  if (getMockMode()) return mockApi.mockGetEconomyStats()
  // No backend aggregate-currency stats endpoint. Needs backend work (see #421).
  throw new Error('Aggregate economy stats are not yet available from the backend (#421).')
}

export async function searchPlayers(query: string, limit: number = 20): Promise<Array<{ playerId: string; email: string; handle: string; currentBalance: number }>> {
  if (getMockMode()) return mockApi.mockSearchPlayers(query, limit)
  // Backend exposes a single-match resolver, not a search. Wrap the match (if any).
  try {
    const res = await apiGet<BackendPlayerLookup>(`/admin/player-lookup/resolve?query=${encodeURIComponent(query)}`)
    const playerId = res.playerId ?? res.userId ?? ''
    if (!playerId) return []
    return [{ playerId, email: res.email ?? '', handle: res.handle ?? '', currentBalance: 0 }]
  } catch {
    // 404 = no match.
    return []
  }
}
