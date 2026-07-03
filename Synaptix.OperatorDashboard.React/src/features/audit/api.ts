/**
 * Audit API client
 *
 * Reconciled to the real backend route surface: /admin/audit/security and
 * /admin/audit/security/{id} (Synaptix.Backend.Api/Features/AdminAnalytics/
 * AdminAuditEndpoints). The backend has no /events, /stats, or /ip-locations
 * routes. Functions keep their existing return types and adapt shapes.
 *
 * Known fidelity gaps (backend does not expose these; see #424):
 *   - Security-audit rows carry id/title/status/createdAt/metadata only, so
 *     AuditEvent.eventType/adminId/resourceId/ipAddress/geo are best-effort
 *     placeholders derived from metadata where present.
 *   - Stats are derived from the list; IP-location data has no backend source
 *     and returns an empty set.
 */

import { apiGet } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type { AuditEvent, AuditEventListResponse, AuditFilter, IPLocationData, SecurityAuditStats } from './types'

interface BackendAuditItemDto {
  id: string
  channelKey: string
  title: string
  status: string
  createdAt: string
  metadata?: Record<string, unknown> | null
}

interface BackendAuditResponse {
  items: BackendAuditItemDto[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

function offsetToPage(offset: number, limit: number): number {
  return Math.floor(offset / Math.max(1, limit)) + 1
}

function metaString(metadata: Record<string, unknown> | null | undefined, key: string): string {
  const v = metadata?.[key]
  return typeof v === 'string' ? v : ''
}

function toAuditEvent(dto: BackendAuditItemDto): AuditEvent {
  return {
    id: dto.id,
    eventType: 'configuration_change',
    adminEmail: metaString(dto.metadata, 'actor') || metaString(dto.metadata, 'adminEmail'),
    adminId: metaString(dto.metadata, 'sub'),
    resourceType: dto.channelKey,
    resourceId: '',
    action: dto.title,
    ipAddress: metaString(dto.metadata, 'ip'),
    userAgent: metaString(dto.metadata, 'userAgent'),
    status: dto.status === 'failed' || dto.status === 'not_found' ? 'failure' : 'success',
    timestamp: dto.createdAt,
    details: (dto.metadata as Record<string, unknown>) ?? undefined,
  }
}

export async function getAuditEvents(filters?: AuditFilter, offset: number = 0, limit: number = 50): Promise<AuditEventListResponse> {
  if (getMockMode()) return mockApi.mockGetAuditEvents(filters, offset, limit)
  const params = new URLSearchParams({
    page: offsetToPage(offset, limit).toString(),
    pageSize: limit.toString(),
  })
  if (filters?.dateFrom) params.set('from', filters.dateFrom)
  if (filters?.dateTo) params.set('to', filters.dateTo)
  const res = await apiGet<BackendAuditResponse>(`/admin/audit/security?${params}`)
  let items = res.items.map(toAuditEvent)
  // Backend list has no admin/status/search filter; apply client-side.
  if (filters?.status) items = items.filter((e) => e.status === filters.status)
  if (filters?.adminEmail) items = items.filter((e) => e.adminEmail.includes(filters.adminEmail!))
  if (filters?.searchText) {
    const query = filters.searchText.toLowerCase()
    items = items.filter((e) => e.action.toLowerCase().includes(query))
  }
  return { items, total: res.totalItems, offset, limit }
}

export async function getAuditStats(): Promise<SecurityAuditStats> {
  if (getMockMode()) return mockApi.mockGetAuditStats()
  // Backend has no /stats; derive from the security list.
  const res = await apiGet<BackendAuditResponse>('/admin/audit/security?page=1&pageSize=200')
  const events = res.items.map(toAuditEvent)
  const successes = events.filter((e) => e.status === 'success').length
  const uniqueAdmins = new Set(events.map((e) => e.adminEmail).filter(Boolean)).size
  const uniqueIPs = new Set(events.map((e) => e.ipAddress).filter(Boolean)).size
  return {
    totalEvents: res.totalItems,
    successRate: events.length === 0 ? 0 : successes / events.length,
    uniqueAdmins,
    uniqueIPs,
  }
}

export async function getIPLocations(_filters?: AuditFilter): Promise<IPLocationData[]> {
  if (getMockMode()) return mockApi.mockGetIPLocations(_filters)
  // No backend geo-IP source (see #424); return an empty set so the map renders empty.
  void _filters
  return []
}

export async function getEventDetail(eventId: string): Promise<AuditEvent> {
  if (getMockMode()) return mockApi.mockGetEventDetail(eventId)
  const dto = await apiGet<BackendAuditItemDto>(`/admin/audit/security/${eventId}`)
  return toAuditEvent(dto)
}
