/**
 * Operations API client
 */

import { apiGet, apiPost } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type { SeasonsListResponse, EventsListResponse, SeasonFilter, EventFilter, LifecycleAction, OperationsStats } from './types'

export async function getSeasons(filters?: SeasonFilter, offset: number = 0, limit: number = 50): Promise<SeasonsListResponse> {
  if (getMockMode()) return mockApi.mockGetSeasons(filters, offset, limit)
  const params = new URLSearchParams({
    offset: offset.toString(),
    limit: limit.toString(),
    ...Object.fromEntries(
      Object.entries(filters || {}).filter(([, v]) => v != null).map(([k, v]) => [k, String(v)])
    ),
  })
  return apiGet(`/admin/operations/seasons?${params}`)
}

export async function getGameEvents(filters?: EventFilter, offset: number = 0, limit: number = 50): Promise<EventsListResponse> {
  if (getMockMode()) return mockApi.mockGetGameEvents(filters, offset, limit)
  const params = new URLSearchParams({
    offset: offset.toString(),
    limit: limit.toString(),
    ...Object.fromEntries(
      Object.entries(filters || {}).filter(([, v]) => v != null).map(([k, v]) => [k, String(v)])
    ),
  })
  return apiGet(`/admin/operations/events?${params}`)
}

export async function performLifecycleAction(action: LifecycleAction): Promise<{ success: boolean; resourceId: string }> {
  if (getMockMode()) return mockApi.mockPerformLifecycleAction(action)
  return apiPost(`/admin/operations/${action.resourceId}/action`, {
    action: action.action,
    notes: action.notes,
  })
}

export async function getOperationsStats(): Promise<OperationsStats> {
  if (getMockMode()) return mockApi.mockGetOperationsStats()
  return apiGet('/admin/operations/stats')
}

export async function getSeason(seasonId: string): Promise<any> {
  if (getMockMode()) return mockApi.mockGetSeason(seasonId)
  return apiGet(`/admin/operations/seasons/${seasonId}`)
}

export async function getEvent(eventId: string): Promise<any> {
  if (getMockMode()) return mockApi.mockGetEvent(eventId)
  return apiGet(`/admin/operations/events/${eventId}`)
}
