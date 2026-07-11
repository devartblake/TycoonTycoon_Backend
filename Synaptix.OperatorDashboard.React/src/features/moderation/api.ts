/**
 * Moderation API client
 */

import { apiGet, apiPost } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type { PlayerModeration, ModerationAction, ModerationLog, ModerationLogListResponse, ModerationLogFilter, ModerationLogStatus } from './types'

export async function getPlayerModeration(playerId: string): Promise<PlayerModeration> {
  if (getMockMode()) return mockApi.mockGetPlayerModeration(playerId)
  return apiGet(`/admin/moderation/players/${playerId}`)
}

export async function banPlayer(playerId: string, reason: string, notes?: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockBanPlayer(playerId, reason, notes)
  return apiPost(`/admin/moderation/players/${playerId}/ban`, { reason, notes })
}

export async function unbanPlayer(playerId: string, reason: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockUnbanPlayer(playerId, reason)
  return apiPost(`/admin/moderation/players/${playerId}/unban`, { reason })
}

export async function suspendPlayer(playerId: string, durationHours: number, reason: string, notes?: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockSuspendPlayer(playerId, durationHours, reason, notes)
  return apiPost(`/admin/moderation/players/${playerId}/suspend`, { durationHours, reason, notes })
}

export async function unsuspendPlayer(playerId: string, reason: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockUnsuspendPlayer(playerId, reason)
  return apiPost(`/admin/moderation/players/${playerId}/unsuspend`, { reason })
}

export async function warnPlayer(playerId: string, reason: string, notes?: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockWarnPlayer(playerId, reason, notes)
  return apiPost(`/admin/moderation/players/${playerId}/warn`, { reason, notes })
}

export async function addModeratorNote(playerId: string, note: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockAddModeratorNote(playerId, note)
  return apiPost(`/admin/moderation/players/${playerId}/note`, { note })
}

// ── Moderation action logs (GET /admin/moderation/logs) ─────────────────────

interface BackendModerationLogItem {
  id: string
  playerId: string
  newStatus: number
  reason: string
  notes?: string | null
  setByAdmin: string
  createdAtUtc: string
  expiresAtUtc?: string | null
  relatedFlagId?: string | null
}

interface BackendModerationLogList {
  page: number
  pageSize: number
  total: number
  items: BackendModerationLogItem[]
}

const STATUS_BY_NUM: Record<number, ModerationLogStatus> = {
  1: 'normal',
  2: 'suspected',
  3: 'restricted',
  4: 'banned',
}

function toModerationLog(item: BackendModerationLogItem): ModerationLog {
  return {
    id: item.id,
    playerId: item.playerId,
    newStatus: STATUS_BY_NUM[item.newStatus] ?? 'normal',
    reason: item.reason,
    notes: item.notes,
    setByAdmin: item.setByAdmin,
    createdAt: item.createdAtUtc,
    expiresAt: item.expiresAtUtc,
    relatedFlagId: item.relatedFlagId,
  }
}

export async function getModerationLogs(filters?: ModerationLogFilter, offset: number = 0, limit: number = 50): Promise<ModerationLogListResponse> {
  if (getMockMode()) return mockApi.mockGetModerationLogs(filters, offset, limit)
  const params = new URLSearchParams({
    page: String(Math.floor(offset / Math.max(1, limit)) + 1),
    pageSize: String(limit),
  })
  if (filters?.playerId) params.set('playerId', filters.playerId)
  if (filters?.status) params.set('status', filters.status)
  const res = await apiGet<BackendModerationLogList>(`/admin/moderation/logs?${params}`)
  return { items: res.items.map(toModerationLog), total: res.total, offset, limit }
}

export async function getModerationLogDetail(logId: string): Promise<ModerationLog> {
  if (getMockMode()) return mockApi.mockGetModerationLogDetail(logId)
  const dto = await apiGet<BackendModerationLogItem>(`/admin/moderation/logs/${logId}`)
  return toModerationLog(dto)
}
