// Sentry error and performance monitoring setup
// For production, replace this with real @sentry/react

interface SentryEvent {
  level?: 'fatal' | 'error' | 'warning' | 'info' | 'debug'
  message?: string
  tags?: Record<string, string>
  contexts?: Record<string, any>
  extra?: Record<string, any>
}

class SentryClient {
  private isProduction = import.meta.env.PROD
  private dsn = import.meta.env.VITE_SENTRY_DSN || ''
  private events: SentryEvent[] = []
  private performanceMetrics: Record<string, number> = {}

  init() {
    if (this.isProduction && this.dsn) {
      console.log('Sentry initialized in production mode')
      this.capturePageLoad()
    } else if (!this.isProduction) {
      console.log('Sentry initialized in development mode (logging to console)')
    }
  }

  captureException(error: Error, context?: Record<string, any>) {
    const event: SentryEvent = {
      level: 'error',
      message: error.message,
      contexts: context,
      extra: {
        stack: error.stack,
        timestamp: new Date().toISOString(),
      },
    }

    this.events.push(event)

    if (this.isProduction) {
      this.sendEvent(event)
    } else {
      console.error('Sentry Error:', event)
    }
  }

  captureMessage(message: string, level: 'error' | 'warning' | 'info' = 'info') {
    const event: SentryEvent = {
      level,
      message,
      extra: { timestamp: new Date().toISOString() },
    }

    this.events.push(event)

    if (this.isProduction) {
      this.sendEvent(event)
    } else {
      console.log(`Sentry ${level}:`, message)
    }
  }

  startSpan(name: string) {
    const startTime = performance.now()

    return {
      end: () => {
        const duration = performance.now() - startTime
        this.performanceMetrics[name] = duration

        if (duration > 1000) {
          // Log slow operations
          this.captureMessage(`Slow operation: ${name} took ${duration}ms`, 'warning')
        }
      },
    }
  }

  capturePageLoad() {
    if ('PerformanceObserver' in window) {
      try {
        const observer = new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) {
            const perfEntry = entry as any
            if (perfEntry.duration > 2000) {
              this.captureMessage(
                `Slow page load: ${perfEntry.name} - ${perfEntry.duration}ms`,
                'warning'
              )
            }
          }
        })

        observer.observe({ entryTypes: ['navigation', 'resource'] })
      } catch (e) {
        // Browser doesn't support PerformanceObserver
      }
    }
  }

  private sendEvent(event: SentryEvent) {
    if (!this.dsn) return

    // In production, send to Sentry endpoint
    fetch(this.dsn, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(event),
    }).catch((err) => console.error('Failed to send Sentry event:', err))
  }

  getMetrics() {
    return this.performanceMetrics
  }

  getEvents() {
    return this.events
  }
}

export const sentry = new SentryClient()

// Initialize on module load
sentry.init()

// Export for use in error boundaries
export const captureException = (error: Error, context?: Record<string, any>) => {
  sentry.captureException(error, context)
}

export const captureMessage = (message: string, level: 'error' | 'warning' | 'info' = 'info') => {
  sentry.captureMessage(message, level)
}

export const startSpan = (name: string) => {
  return sentry.startSpan(name)
}
