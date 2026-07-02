/**
 * Dashboard feature types
 */

export type ServiceStatus = 'healthy' | 'degraded' | 'offline'

export interface ServiceHealth {
  id: string
  name: string
  displayName: string
  status: ServiceStatus
  uptime: number // percentage
  responseTime: number // ms
  lastCheckedAt: string
  nextCheckAt: string
  description: string
  endpoint?: string
}

export interface HealthMetric {
  timestamp: string
  value: number // uptime or response time
}

export interface SystemMetrics {
  apiGatewayRequests: number
  activeConnections: number
  cpuUsage: number // percentage
  memoryUsage: number // percentage
  diskUsage: number // percentage
  avgResponseTime: number // ms
  errorRate: number // percentage
}

export interface DashboardStats {
  services: ServiceHealth[]
  metrics: SystemMetrics
  lastUpdatedAt: string
  checksPerformed: number
  alertsActive: number
}

export interface ServiceHistory {
  serviceId: string
  metrics: HealthMetric[]
}
