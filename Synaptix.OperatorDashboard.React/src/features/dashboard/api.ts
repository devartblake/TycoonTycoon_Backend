/**
 * Dashboard API client
 *
 * The overview is served by /admin/dashboard/stats (#418), which aggregates the
 * backend's registered health checks into the service list. Rich system metrics
 * and per-service time-series history are not queryable from the API (they live
 * in Prometheus/Grafana), so `metrics` is a zeroed placeholder and the history
 * calls below return empty until a metrics integration is added.
 */

import { apiGet } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type { DashboardStats, ServiceHistory } from './types'

export async function getDashboardStats(): Promise<DashboardStats> {
  if (getMockMode()) return mockApi.mockGetDashboardStats()
  return apiGet<DashboardStats>('/admin/dashboard/stats')
}

export async function getServiceHistory(serviceId: string, _hours: number = 24): Promise<ServiceHistory> {
  if (getMockMode()) return mockApi.mockGetServiceHistory(serviceId, _hours)
  // No backend time-series source (see #418).
  void _hours
  return { serviceId, metrics: [] }
}

export async function getAllServiceHistory(_hours: number = 24): Promise<ServiceHistory[]> {
  if (getMockMode()) return mockApi.mockGetAllServiceHistory(_hours)
  // No backend time-series source (see #418).
  void _hours
  return []
}
