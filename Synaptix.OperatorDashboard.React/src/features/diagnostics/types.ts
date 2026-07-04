/**
 * Diagnostics & Probe Log types
 */

export interface ProbeLog {
  id: string
  timestamp: string
  probeId: string
  probeName: string
  status: 'success' | 'warning' | 'error' | 'critical'
  duration: number
  message: string
  details?: Record<string, any>
  endpoint: string
  responseTime: number
  statusCode: number
}

export interface ProbeRecord {
  id: string
  probeId: string
  name: string
  type: 'http' | 'ping' | 'dns' | 'custom'
  interval: number
  enabled: boolean
  lastRun?: string
  lastStatus: 'healthy' | 'degraded' | 'unhealthy'
  successRate: number
  avgResponseTime: number
  uptime: number
}

export interface DiagnosticMetrics {
  totalProbes: number
  activeProbes: number
  healthyProbes: number
  degradedProbes: number
  unhealthyProbes: number
  averageResponseTime: number
  overallUptime: number
  totalRequests: number
  failedRequests: number
}

export interface ProbeHistoryItem {
  timestamp: string
  status: 'success' | 'warning' | 'error' | 'critical'
  responseTime: number
  statusCode: number
}

export type LogLevel = 'debug' | 'info' | 'warn' | 'error' | 'critical'
export type ProbeStatus = 'healthy' | 'degraded' | 'unhealthy'
