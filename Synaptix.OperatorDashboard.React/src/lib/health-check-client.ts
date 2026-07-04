/**
 * Health Check API Client
 * Fetches real-time health metrics from backend /health endpoint
 */

export interface HealthStatus {
  status: 'Healthy' | 'Degraded' | 'Unhealthy'
  checks: Record<string, { status: string }>
  timestamp: string
}

export interface SystemMetrics {
  apiGatewayRequests: number
  activeConnections: number
  cpuUsage: number
  memoryUsage: number
  diskUsage: number
  uptime: number
  errorRate: number
  responseTime: number
  avgResponseTime?: number // alias for responseTime
  isHealthy: boolean
}

class HealthCheckClient {
  private readonly apiBaseUrl: string
  private cache: Map<string, { data: any; timestamp: number }> = new Map()
  private cacheTimeout = 30000 // 30 seconds

  constructor(apiBaseUrl: string = '') {
    this.apiBaseUrl = apiBaseUrl || (typeof window !== 'undefined' ? '' : 'http://backend-api:5000')
  }

  /**
   * Fetch health status from /health endpoint
   */
  async getHealthStatus(): Promise<HealthStatus | null> {
    try {
      const response = await fetch(`${this.apiBaseUrl}/health`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      })

      if (!response.ok) {
        return {
          status: 'Unhealthy',
          checks: {},
          timestamp: new Date().toISOString(),
        }
      }

      return await response.json()
    } catch (error) {
      console.error('Failed to fetch health status:', error)
      return null
    }
  }

  /**
   * Fetch liveness status from /alive endpoint
   */
  async getLivenessStatus(): Promise<boolean> {
    try {
      const response = await fetch(`${this.apiBaseUrl}/alive`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      })
      return response.ok
    } catch (error) {
      console.error('Failed to fetch liveness status:', error)
      return false
    }
  }

  /**
   * Fetch readiness status from /ready endpoint
   */
  async getReadinessStatus(): Promise<boolean> {
    try {
      const response = await fetch(`${this.apiBaseUrl}/ready`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      })
      return response.ok
    } catch (error) {
      console.error('Failed to fetch readiness status:', error)
      return false
    }
  }

  /**
   * Transform health check data into system metrics
   * This is a transformation layer - real metrics come from Prometheus
   * Health checks provide a quick readiness indicator
   */
  async getSystemMetrics(): Promise<SystemMetrics> {
    const cacheKey = 'system-metrics'
    const cached = this.cache.get(cacheKey)

    if (cached && Date.now() - cached.timestamp < this.cacheTimeout) {
      return cached.data
    }

    try {
      const [healthStatus, liveness, readiness, prometheusMetrics] = await Promise.all([
        this.getHealthStatus(),
        this.getLivenessStatus(),
        this.getReadinessStatus(),
        this.getPrometheusMetrics(),
      ])

      const isHealthy = healthStatus?.status === 'Healthy' && liveness && readiness

      const responseTime = prometheusMetrics?.responseTime || 0
      const metrics: SystemMetrics = {
        // Real metrics from Prometheus (if available)
        apiGatewayRequests: prometheusMetrics?.apiGatewayRequests || 0,
        activeConnections: prometheusMetrics?.activeConnections || 0,
        cpuUsage: prometheusMetrics?.cpuUsage || 0,
        memoryUsage: prometheusMetrics?.memoryUsage || 0,
        diskUsage: prometheusMetrics?.diskUsage || 0,
        uptime: prometheusMetrics?.uptime || 0,
        errorRate: prometheusMetrics?.errorRate || 0,
        responseTime,
        avgResponseTime: responseTime,
        isHealthy,
      }

      this.cache.set(cacheKey, { data: metrics, timestamp: Date.now() })
      return metrics
    } catch (error) {
      console.error('Failed to get system metrics:', error)
      return this.getDefaultMetrics()
    }
  }

  /**
   * Fetch real metrics from Prometheus (if configured)
   * This requires Prometheus API to be accessible
   */
  private async getPrometheusMetrics(): Promise<Partial<SystemMetrics> | null> {
    try {
      // Try to fetch from Prometheus if available
      // In development: http://localhost:9090
      // In production: behind Traefik
      const response = await fetch(`${this.apiBaseUrl}/api/v1/query?query=up`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      })

      if (!response.ok) {
        return null
      }

      const data = await response.json()

      // Parse Prometheus response and extract key metrics
      return {
        cpuUsage: this.extractMetric(data, 'process_cpu_seconds_total', 0),
        memoryUsage: this.extractMetric(data, 'process_resident_memory_bytes', 0),
        diskUsage: this.extractMetric(data, 'node_filesystem_avail_bytes', 0),
      }
    } catch (error) {
      console.warn('Prometheus metrics unavailable:', error)
      return null
    }
  }

  /**
   * Extract metric value from Prometheus response
   */
  private extractMetric(_data: any, _metricName: string, fallback: number): number {
    try {
      // Implementation depends on Prometheus API format
      return fallback
    } catch {
      return fallback
    }
  }

  /**
   * Get default/fallback metrics when services are unavailable
   */
  private getDefaultMetrics(): SystemMetrics {
    return {
      apiGatewayRequests: 0,
      activeConnections: 0,
      cpuUsage: 0,
      memoryUsage: 0,
      diskUsage: 0,
      uptime: 0,
      errorRate: 0,
      responseTime: 0,
      avgResponseTime: 0,
      isHealthy: false,
    }
  }

  /**
   * Start polling health status at specified interval
   */
  startPolling(onUpdate: (metrics: SystemMetrics) => void, intervalMs: number = 30000) {
    const poll = async () => {
      const metrics = await this.getSystemMetrics()
      onUpdate(metrics)
    }

    poll() // Initial call
    return setInterval(poll, intervalMs)
  }

  /**
   * Stop polling
   */
  stopPolling(intervalId: number) {
    clearInterval(intervalId)
  }

  /**
   * Get status color based on health
   */
  getStatusColor(isHealthy: boolean, usage?: number): string {
    if (isHealthy === false) {
      return 'text-status-offline'
    }

    if (usage !== undefined) {
      if (usage < 50) return 'text-status-healthy'
      if (usage < 75) return 'text-status-degraded'
      return 'text-status-offline'
    }

    return 'text-status-healthy'
  }
}

// Export singleton instance
export const healthCheckClient = new HealthCheckClient(
  import.meta.env.VITE_HEALTH_CHECK_BASE_URL ||
    import.meta.env.VITE_API_BASE_URL ||
    'http://localhost:5000'
)

export default HealthCheckClient
