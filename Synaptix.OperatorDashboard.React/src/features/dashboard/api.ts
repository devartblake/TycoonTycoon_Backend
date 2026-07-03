/**
 * Dashboard API client
 *
 * The backend has no /admin/dashboard group. The only operator-reachable health
 * surface on the .NET API is /healthz (+ /health/ready); rich system metrics and
 * per-service time-series live in Prometheus/Grafana, which this dashboard cannot
 * query directly. So the overview is derived from the health endpoint with
 * placeholder metrics, and history returns empty until a backend aggregation is
 * built (see #418).
 */

import { apiGet } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type { DashboardStats, ServiceHistory, ServiceHealth, SystemMetrics } from './types'

interface HealthzResponse {
  status: string
  timestamp: string
}

const EMPTY_METRICS: SystemMetrics = {
  apiGatewayRequests: 0,
  activeConnections: 0,
  cpuUsage: 0,
  memoryUsage: 0,
  diskUsage: 0,
  avgResponseTime: 0,
  errorRate: 0,
}

export async function getDashboardStats(): Promise<DashboardStats> {
  if (getMockMode()) return mockApi.mockGetDashboardStats()
  const now = new Date().toISOString()

  let apiStatus: ServiceHealth['status'] = 'offline'
  try {
    const health = await apiGet<HealthzResponse>('/healthz')
    apiStatus = health.status === 'healthy' ? 'healthy' : 'degraded'
  } catch {
    apiStatus = 'offline'
  }

  const apiService: ServiceHealth = {
    id: 'backend-api',
    name: 'backend-api',
    displayName: 'Backend API',
    status: apiStatus,
    uptime: 0,
    responseTime: 0,
    lastCheckedAt: now,
    nextCheckAt: now,
    description: 'Synaptix Backend API (/healthz)',
    endpoint: '/healthz',
  }

  return {
    services: [apiService],
    metrics: EMPTY_METRICS,
    lastUpdatedAt: now,
    checksPerformed: 1,
    alertsActive: apiStatus === 'offline' ? 1 : 0,
  }
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
