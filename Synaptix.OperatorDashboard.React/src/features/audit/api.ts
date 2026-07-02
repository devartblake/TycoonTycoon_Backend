/**
 * Audit API client
 */

import { apiGet } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type { AuditEventListResponse, AuditFilter, IPLocationData, SecurityAuditStats } from './types'

export async function getAuditEvents(filters?: AuditFilter, offset: number = 0, limit: number = 50): Promise<AuditEventListResponse> {
  if (getMockMode()) return mockApi.mockGetAuditEvents(filters, offset, limit)
  const params = new URLSearchParams({
    offset: offset.toString(),
    limit: limit.toString(),
    ...Object.fromEntries(
      Object.entries(filters || {}).filter(([, v]) => v != null).map(([k, v]) => [k, String(v)])
    ),
  })
  return apiGet(`/admin/audit/events?${params}`)
}

export async function getAuditStats(): Promise<SecurityAuditStats> {
  if (getMockMode()) return mockApi.mockGetAuditStats()
  return apiGet('/admin/audit/stats')
}

export async function getIPLocations(filters?: AuditFilter): Promise<IPLocationData[]> {
  if (getMockMode()) return mockApi.mockGetIPLocations(filters)
  const params = new URLSearchParams(
    Object.entries(filters || {}).filter(([, v]) => v != null).map(([k, v]) => [k, String(v)])
  )
  return apiGet(`/admin/audit/ip-locations?${params}`)
}

export async function getEventDetail(eventId: string): Promise<any> {
  if (getMockMode()) return mockApi.mockGetEventDetail(eventId)
  return apiGet(`/admin/audit/events/${eventId}`)
}
