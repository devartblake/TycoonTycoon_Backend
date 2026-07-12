/**
 * Dashboard API client
 *
 * The overview is served by /admin/dashboard/stats (#418), which aggregates the
 * backend's registered health checks into the service list. Per-service history
 * comes from /admin/dashboard/services/history — an in-memory sampler on the
 * backend (metric value = check response time in ms; resets on API restart).
 * Rich system metrics (CPU/memory/error-rate) still live in Prometheus/Grafana
 * and are not queryable from the API, so `metrics` is a zeroed placeholder.
 */

import { apiGet } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type { DashboardStats, ServiceHistory } from './types'

export async function getDashboardStats(): Promise<DashboardStats> {
  if (getMockMode()) return mockApi.mockGetDashboardStats()
  return apiGet<DashboardStats>('/admin/dashboard/stats')
}

export async function getServiceHistory(serviceId: string, hours: number = 24): Promise<ServiceHistory> {
  if (getMockMode()) return mockApi.mockGetServiceHistory(serviceId, hours)
  return apiGet<ServiceHistory>(`/admin/dashboard/services/${encodeURIComponent(serviceId)}/history?hours=${hours}`)
}

export async function getAllServiceHistory(hours: number = 24): Promise<ServiceHistory[]> {
  if (getMockMode()) return mockApi.mockGetAllServiceHistory(hours)
  return apiGet<ServiceHistory[]>(`/admin/dashboard/services/history?hours=${hours}`)
}
