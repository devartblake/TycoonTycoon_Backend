/**
 * Frontend admin analytics emitter.
 *
 * Tracks admin API request outcomes (success/failure) with dimensions:
 *   - endpoint path
 *   - error.code (from backend security envelope)
 *   - HTTP status
 *   - latency (ms)
 *
 * Data is stored in-memory for the current session and exposed via
 * getAnalyticsSnapshot() for the observability dashboard. In production,
 * the emit() call can be extended to forward events to an external
 * analytics service (e.g. Datadog, Sentry, Mixpanel).
 */

// ─── Types ──────────────────────────────────────────────────────────

export interface AdminApiEvent {
  timestamp: number
  endpoint: string
  method: string
  status: number
  errorCode: string | null
  latencyMs: number
  success: boolean
}

export interface ErrorCodeCount {
  code: string
  count: number
}

export interface EndpointStats {
  endpoint: string
  totalRequests: number
  failures: number
  avgLatencyMs: number
}

export interface AnalyticsSnapshot {
  totalRequests: number
  totalFailures: number
  successRate: number
  throttledCount: number
  avgLatencyMs: number
  errorsByCode: ErrorCodeCount[]
  recentEvents: AdminApiEvent[]
  endpointStats: EndpointStats[]
}

// ─── In-memory event buffer ─────────────────────────────────────────

const MAX_EVENTS = 500
const events: AdminApiEvent[] = []

// ─── Public API ─────────────────────────────────────────────────────

export function emit(event: AdminApiEvent): void {
  events.push(event)

  // Trim oldest events when buffer exceeds limit
  if (events.length > MAX_EVENTS) {
    events.splice(0, events.length - MAX_EVENTS)
  }
}

export function getAnalyticsSnapshot(): AnalyticsSnapshot {
  const total = events.length

  if (total === 0) {
    return {
      totalRequests: 0,
      totalFailures: 0,
      successRate: 100,
      throttledCount: 0,
      avgLatencyMs: 0,
      errorsByCode: [],
      recentEvents: [],
      endpointStats: []
    }
  }

  const failures = events.filter(e => !e.success)
  const throttled = events.filter(e => e.errorCode === 'RATE_LIMITED')
  const totalLatency = events.reduce((sum, e) => sum + e.latencyMs, 0)

  // Errors grouped by code
  const codeMap = new Map<string, number>()

  for (const e of failures) {
    const key = e.errorCode ?? `HTTP_${e.status}`

    codeMap.set(key, (codeMap.get(key) ?? 0) + 1)
  }

  const errorsByCode: ErrorCodeCount[] = Array.from(codeMap.entries())
    .map(([code, count]) => ({ code, count }))
    .sort((a, b) => b.count - a.count)

  // Per-endpoint stats
  const epMap = new Map<string, { total: number; failures: number; latency: number }>()

  for (const e of events) {
    const entry = epMap.get(e.endpoint) ?? { total: 0, failures: 0, latency: 0 }

    entry.total++
    entry.latency += e.latencyMs
    if (!e.success) entry.failures++
    epMap.set(e.endpoint, entry)
  }

  const endpointStats: EndpointStats[] = Array.from(epMap.entries())
    .map(([endpoint, s]) => ({
      endpoint,
      totalRequests: s.total,
      failures: s.failures,
      avgLatencyMs: Math.round(s.total > 0 ? s.latency / s.total : 0)
    }))
    .sort((a, b) => b.failures - a.failures)

  return {
    totalRequests: total,
    totalFailures: failures.length,
    successRate: Math.round(((total - failures.length) / total) * 1000) / 10,
    throttledCount: throttled.length,
    avgLatencyMs: Math.round(totalLatency / total),
    errorsByCode,
    recentEvents: events.slice(-50).reverse(),
    endpointStats
  }
}

export function clearAnalytics(): void {
  events.length = 0
}
