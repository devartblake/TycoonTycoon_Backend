/**
 * Operations API client
 *
 * Reconciled to the real backend route surface: seasons live under
 * /admin/seasons (Synaptix.Backend.Api/Features/AdminSeasons) and game events
 * under /admin/game-events (Features/GameEvents/AdminGameEventsEndpoints). There
 * is no /admin/operations group. Functions keep their existing return types and
 * adapt the backend shapes internally.
 *
 * Known fidelity gaps (backend does not expose these fields today; see the
 * operations reconciliation sub-issue #419):
 *   - Season.description / rewardPool / pointsMultiplier / createdBy and
 *     GameEvent.name / description / participantCount / endDate / createdBy are
 *     best-effort placeholders.
 *   - Aggregated OperationsStats is derived from the season/event lists.
 */

import { apiGet, apiPost } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type {
  SeasonsListResponse,
  EventsListResponse,
  SeasonFilter,
  EventFilter,
  LifecycleAction,
  OperationsStats,
  Season,
  GameEvent,
  SeasonStatus,
  EventStatus,
} from './types'

// ── Backend DTO shapes (camelCase) ───────────────────────────────────────────

interface BackendSeasonDto {
  id: string
  seasonNumber: number
  name: string
  status: string
  startsAtUtc: string
  endsAtUtc: string
}

interface BackendSeasonListResponse {
  page: number
  pageSize: number
  total: number
  items: BackendSeasonDto[]
}

interface BackendGameEventDto {
  id: string
  kind: string
  tierId: string | null
  status: string
  scheduledAtUtc: string
  entryFeeCoins: number
  maxParticipants: number
}

interface BackendGameEventListResponse {
  page: number
  pageSize: number
  total: number
  items: BackendGameEventDto[]
}

// ── Mapping helpers ──────────────────────────────────────────────────────────

function offsetToPage(offset: number, limit: number): number {
  return Math.floor(offset / Math.max(1, limit)) + 1
}

function mapSeasonStatus(status: string): SeasonStatus {
  switch (status.toLowerCase()) {
    case 'active':
      return 'active'
    case 'closed':
    case 'ended':
      return 'ended'
    case 'scheduled':
      return 'scheduled'
    default:
      return 'draft'
  }
}

function toSeason(dto: BackendSeasonDto): Season {
  return {
    id: dto.id,
    name: dto.name,
    description: '',
    status: mapSeasonStatus(dto.status),
    number: dto.seasonNumber,
    startDate: dto.startsAtUtc,
    endDate: dto.endsAtUtc,
    rewardPool: 0,
    pointsMultiplier: 1,
    createdAt: dto.startsAtUtc,
    createdBy: '',
  }
}

function mapEventStatus(status: string): EventStatus {
  switch (status.toLowerCase()) {
    case 'active':
    case 'open':
      return 'active'
    case 'scheduled':
    case 'upcoming':
      return 'upcoming'
    case 'closed':
    case 'ended':
      return 'ended'
    case 'cancelled':
    case 'canceled':
      return 'cancelled'
    default:
      return 'draft'
  }
}

function mapEventType(kind: string): GameEvent['type'] {
  const k = kind.toLowerCase()
  if (k.includes('tournament')) return 'tournament'
  if (k.includes('challenge')) return 'challenge'
  if (k.includes('promo')) return 'promotion'
  return 'special'
}

function toGameEvent(dto: BackendGameEventDto): GameEvent {
  return {
    id: dto.id,
    name: dto.kind,
    description: '',
    type: mapEventType(dto.kind),
    status: mapEventStatus(dto.status),
    startDate: dto.scheduledAtUtc,
    endDate: dto.scheduledAtUtc,
    reward: dto.entryFeeCoins,
    participantCount: 0,
    maxParticipants: dto.maxParticipants,
    createdAt: dto.scheduledAtUtc,
    createdBy: '',
  }
}

// ── Seasons ──────────────────────────────────────────────────────────────────

export async function getSeasons(filters?: SeasonFilter, offset: number = 0, limit: number = 50): Promise<SeasonsListResponse> {
  if (getMockMode()) return mockApi.mockGetSeasons(filters, offset, limit)
  const params = new URLSearchParams({
    page: offsetToPage(offset, limit).toString(),
    pageSize: limit.toString(),
  })
  const res = await apiGet<BackendSeasonListResponse>(`/admin/seasons?${params}`)
  let items = res.items.map(toSeason)
  // Backend has no season status/search filter; apply client-side to preserve UX.
  if (filters?.status) items = items.filter((s) => s.status === filters.status)
  if (filters?.searchText) {
    const q = filters.searchText.toLowerCase()
    items = items.filter((s) => s.name.toLowerCase().includes(q))
  }
  return { items, total: res.total, offset, limit }
}

export async function getSeason(seasonId: string): Promise<Season> {
  if (getMockMode()) return mockApi.mockGetSeason(seasonId)
  // Backend has no single-season GET; resolve from the list.
  const res = await apiGet<BackendSeasonListResponse>('/admin/seasons?page=1&pageSize=200')
  const dto = res.items.find((s) => s.id === seasonId)
  if (!dto) throw new Error(`Season ${seasonId} not found`)
  return toSeason(dto)
}

// ── Game events ──────────────────────────────────────────────────────────────

export async function getGameEvents(filters?: EventFilter, offset: number = 0, limit: number = 50): Promise<EventsListResponse> {
  if (getMockMode()) return mockApi.mockGetGameEvents(filters, offset, limit)
  const params = new URLSearchParams({
    page: offsetToPage(offset, limit).toString(),
    pageSize: limit.toString(),
  })
  if (filters?.status) params.set('status', filters.status)
  const res = await apiGet<BackendGameEventListResponse>(`/admin/game-events/?${params}`)
  let items = res.items.map(toGameEvent)
  // Backend list filters only by status; type/search applied client-side.
  if (filters?.type) items = items.filter((e) => e.type === filters.type)
  if (filters?.searchText) {
    const q = filters.searchText.toLowerCase()
    items = items.filter((e) => e.name.toLowerCase().includes(q))
  }
  return { items, total: res.total, offset, limit }
}

export async function getEvent(eventId: string): Promise<GameEvent> {
  if (getMockMode()) return mockApi.mockGetEvent(eventId)
  // Backend has no single-event GET; resolve from the list.
  const res = await apiGet<BackendGameEventListResponse>('/admin/game-events/?page=1&pageSize=200')
  const dto = res.items.find((e) => e.id === eventId)
  if (!dto) throw new Error(`Game event ${eventId} not found`)
  return toGameEvent(dto)
}

// ── Lifecycle ──────────────────────────────────────────────────────────────────

export async function performLifecycleAction(action: LifecycleAction): Promise<{ success: boolean; resourceId: string }> {
  if (getMockMode()) return mockApi.mockPerformLifecycleAction(action)

  if (action.resourceType === 'season') {
    switch (action.action) {
      case 'start':
        await apiPost('/admin/seasons/activate', { seasonId: action.resourceId })
        break
      case 'close':
        await apiPost(`/admin/seasons/${action.resourceId}/close`, {})
        break
      default:
        throw new Error(`Season lifecycle action "${action.action}" is not supported.`)
    }
  } else {
    switch (action.action) {
      case 'start':
        // Game events open then start; "start" from the dashboard opens the event.
        await apiPost(`/admin/game-events/${action.resourceId}/open`, {})
        break
      case 'close':
        await apiPost(`/admin/game-events/${action.resourceId}/close`, {})
        break
      case 'cancel':
        await apiPost(`/admin/game-events/${action.resourceId}/cancel`, {})
        break
    }
  }

  return { success: true, resourceId: action.resourceId }
}

// ── Stats (aggregated from the lists; no backend /operations/stats) ──────────

export async function getOperationsStats(): Promise<OperationsStats> {
  if (getMockMode()) return mockApi.mockGetOperationsStats()
  const [seasonsRes, eventsRes] = await Promise.all([
    apiGet<BackendSeasonListResponse>('/admin/seasons?page=1&pageSize=200'),
    apiGet<BackendGameEventListResponse>('/admin/game-events/?page=1&pageSize=200'),
  ])
  const seasons = seasonsRes.items.map(toSeason)
  const events = eventsRes.items.map(toGameEvent)
  return {
    activeSeasons: seasons.filter((s) => s.status === 'active').length,
    upcomingEvents: events.filter((e) => e.status === 'upcoming').length,
    totalParticipants: events.reduce((sum, e) => sum + e.participantCount, 0),
    rewardPoolRemaining: seasons.reduce((sum, s) => sum + s.rewardPool, 0),
  }
}
