/**
 * Moderation API client — aligned to backend + Django operator clients.
 *
 * Backend surface (AdminModerationEndpoints + AdminUsersEndpoints):
 *   GET  /admin/moderation/profile/{playerId}
 *   POST /admin/moderation/set-status
 *   GET  /admin/moderation/logs
 *   GET  /admin/moderation/logs/{id}
 *   GET  /admin/users/{userId}          (enrichment for handle/email)
 *   GET  /admin/users/{userId}/activity (activity timeline)
 *
 * There are no /admin/moderation/players/{id}/ban|suspend|… routes.
 * UI actions map onto ModerationStatus via set-status (same as Django).
 */

import { apiGet, apiPost } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type {
  PlayerModeration,
  PlayerProfile,
  ModerationAction,
  PlayerActivity,
  ModerationLog,
  ModerationLogListResponse,
  ModerationLogFilter,
  ModerationLogStatus,
} from './types'

/** Backend ModerationStatus enum */
const ModerationStatus = {
  Normal: 1,
  Suspected: 2,
  Restricted: 3,
  Banned: 4,
} as const

interface BackendModerationProfileDto {
  playerId: string
  status: number
  reason?: string | null
  notes?: string | null
  setByAdmin?: string | null
  setAtUtc: string
  expiresAtUtc?: string | null
}

interface BackendUserDetailDto {
  id: string
  username: string
  email: string
  status: string
  isBanned: boolean
  createdAt: string
  lastActive: string | null
  totalGamesPlayed?: number
  totalPoints?: number
  winRate?: number
}

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

interface BackendActivityResponse {
  items: Array<{
    id: string
    type: string
    description: string
    createdAt: string
    metadata?: Record<string, unknown>
  }>
  page: number
  pageSize: number
  totalItems: number
}

const STATUS_BY_NUM: Record<number, ModerationLogStatus> = {
  1: 'normal',
  2: 'suspected',
  3: 'restricted',
  4: 'banned',
}

function profileStatusFromModeration(status: number, isBanned?: boolean): PlayerProfile['status'] {
  if (isBanned || status === ModerationStatus.Banned) return 'banned'
  if (status === ModerationStatus.Restricted) return 'suspended'
  return 'active'
}

function actionFromLog(item: BackendModerationLogItem): ModerationAction {
  const statusLabel = STATUS_BY_NUM[item.newStatus] ?? 'normal'
  const actionMap: Record<ModerationLogStatus, ModerationAction['action']> = {
    normal: 'unban',
    suspected: 'warn',
    restricted: 'suspend',
    banned: 'ban',
  }
  return {
    id: item.id,
    playerId: item.playerId,
    adminEmail: item.setByAdmin || 'unknown',
    action: actionMap[statusLabel],
    reason: item.reason || '',
    notes: item.notes ?? undefined,
    expiresAt: item.expiresAtUtc ?? undefined,
    status: item.expiresAtUtc && new Date(item.expiresAtUtc) < new Date() ? 'expired' : 'active',
    createdAt: item.createdAtUtc,
  }
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

/**
 * Compose player moderation view from profile + logs + optional user detail/activity.
 */
export async function getPlayerModeration(playerId: string): Promise<PlayerModeration> {
  if (getMockMode()) return mockApi.mockGetPlayerModeration(playerId)

  const [modProfile, logsRes, user, activityRes] = await Promise.all([
    apiGet<BackendModerationProfileDto>(`/admin/moderation/profile/${playerId}`),
    apiGet<BackendModerationLogList>(
      `/admin/moderation/logs?page=1&pageSize=50&playerId=${encodeURIComponent(playerId)}`
    ).catch((): BackendModerationLogList => ({ page: 1, pageSize: 50, total: 0, items: [] })),
    apiGet<BackendUserDetailDto>(`/admin/users/${playerId}`).catch(() => null),
    apiGet<BackendActivityResponse>(
      `/admin/users/${playerId}/activity?page=1&pageSize=30`
    ).catch((): BackendActivityResponse => ({
      items: [],
      page: 1,
      pageSize: 30,
      totalItems: 0,
    })),
  ])

  const actions = logsRes.items.map(actionFromLog)
  const profile: PlayerProfile = {
    id: playerId,
    email: user?.email ?? '',
    handle: user?.username ?? playerId.slice(0, 8),
    status: profileStatusFromModeration(modProfile.status, user?.isBanned),
    createdAt: user?.createdAt ?? modProfile.setAtUtc,
    lastActiveAt: user?.lastActive ?? null,
    flagCount: actions.filter((a) => a.action === 'warn').length,
    accountBalance: 0,
    totalSpent: 0,
    playtimeHours: 0,
    winRate: user?.winRate != null ? Number(user.winRate) * 100 : 0,
    trustScore:
      modProfile.status === ModerationStatus.Banned
        ? 10
        : modProfile.status === ModerationStatus.Restricted
          ? 40
          : modProfile.status === ModerationStatus.Suspected
            ? 60
            : 90,
  }

  const activity: PlayerActivity[] = activityRes.items.map((a) => ({
    id: a.id,
    playerId,
    type: mapActivityType(a.type),
    description: a.description,
    metadata: a.metadata,
    timestamp: a.createdAt,
  }))

  return {
    profile,
    actions,
    activity,
    stats: {
      totalWarnings: actions.filter((a) => a.action === 'warn').length,
      totalBans: actions.filter((a) => a.action === 'ban').length,
      lastAction: actions[0],
    },
  }
}

function mapActivityType(type: string): PlayerActivity['type'] {
  const t = type.toLowerCase()
  if (t.includes('login')) return 'login'
  if (t.includes('purchase') || t.includes('buy')) return 'purchase'
  if (t.includes('violat') || t.includes('flag')) return 'violation'
  if (t.includes('appeal')) return 'appeal'
  if (t.includes('action') || t.includes('moderat')) return 'action'
  return 'game_played'
}

async function setStatus(
  playerId: string,
  status: number,
  reason: string,
  notes?: string,
  expiresAtUtc?: string | null
): Promise<ModerationAction> {
  const dto = await apiPost<BackendModerationProfileDto>('/admin/moderation/set-status', {
    playerId,
    status,
    reason,
    notes: notes ?? null,
    expiresAtUtc: expiresAtUtc ?? null,
    relatedFlagId: null,
  })

  const actionMap: Record<number, ModerationAction['action']> = {
    [ModerationStatus.Normal]: 'unban',
    [ModerationStatus.Suspected]: 'warn',
    [ModerationStatus.Restricted]: 'suspend',
    [ModerationStatus.Banned]: 'ban',
  }

  return {
    id: `status-${dto.setAtUtc}`,
    playerId: dto.playerId,
    adminEmail: dto.setByAdmin || 'operator',
    action: actionMap[dto.status] ?? 'note',
    reason: dto.reason || reason,
    notes: dto.notes ?? notes,
    expiresAt: dto.expiresAtUtc ?? undefined,
    status: 'active',
    createdAt: dto.setAtUtc,
  }
}

export async function banPlayer(playerId: string, reason: string, notes?: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockBanPlayer(playerId, reason, notes)
  return setStatus(playerId, ModerationStatus.Banned, reason, notes)
}

export async function unbanPlayer(playerId: string, reason: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockUnbanPlayer(playerId, reason)
  return setStatus(playerId, ModerationStatus.Normal, reason || 'Unbanned by operator')
}

/** Temporary restriction (backend Restricted = 3), not a separate suspend route. */
export async function suspendPlayer(
  playerId: string,
  durationHours: number,
  reason: string,
  notes?: string
): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockSuspendPlayer(playerId, durationHours, reason, notes)
  const expires = new Date(Date.now() + durationHours * 60 * 60 * 1000).toISOString()
  return setStatus(playerId, ModerationStatus.Restricted, reason, notes, expires)
}

export async function unsuspendPlayer(playerId: string, reason: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockUnsuspendPlayer(playerId, reason)
  return setStatus(playerId, ModerationStatus.Normal, reason || 'Restriction lifted')
}

/** Maps to Suspected (2) — closest backend status for a formal warning. */
export async function warnPlayer(playerId: string, reason: string, notes?: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockWarnPlayer(playerId, reason, notes)
  return setStatus(playerId, ModerationStatus.Suspected, reason, notes)
}

/** Note without changing status: re-apply current status with new notes when possible. */
export async function addModeratorNote(playerId: string, note: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockAddModeratorNote(playerId, note)
  const current = await apiGet<BackendModerationProfileDto>(`/admin/moderation/profile/${playerId}`)
  return setStatus(
    playerId,
    current.status || ModerationStatus.Normal,
    current.reason || 'Operator note',
    note,
    current.expiresAtUtc
  )
}

// ── Moderation action logs ───────────────────────────────────────────────────

export async function getModerationLogs(
  filters?: ModerationLogFilter,
  offset: number = 0,
  limit: number = 50
): Promise<ModerationLogListResponse> {
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
