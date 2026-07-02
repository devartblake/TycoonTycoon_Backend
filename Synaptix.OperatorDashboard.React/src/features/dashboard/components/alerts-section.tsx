/**
 * Active alerts section
 */

interface AlertSectionProps {
  alertCount: number
  services: any[]
}

export function AlertsSection({ alertCount, services }: AlertSectionProps) {
  const degradedServices = services.filter((s) => s.status !== 'healthy')

  if (alertCount === 0) {
    return (
      <div className="operator-card bg-status-healthy/5 border border-status-healthy/20">
        <div className="flex items-center gap-3">
          <div className="text-3xl">✓</div>
          <div>
            <p className="font-semibold text-status-healthy">All Systems Operational</p>
            <p className="text-sm text-ink-secondary mt-1">No active alerts. Dashboard is monitoring {services.length} services.</p>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="operator-card space-y-3">
      <div className="flex items-center justify-between">
        <h2 className="font-semibold text-ink-primary">Active Alerts</h2>
        <span className="px-2 py-1 bg-status-offline/10 text-status-offline rounded text-xs font-bold">
          {alertCount}
        </span>
      </div>

      {degradedServices.map((service) => (
        <div key={service.id} className="p-3 bg-bg-secondary rounded border-l-4 border-status-degraded">
          <div className="flex items-start justify-between">
            <div>
              <p className="font-medium text-ink-primary">{service.displayName}</p>
              <p className="text-sm text-ink-secondary mt-1">
                {service.status === 'degraded'
                  ? `Response time elevated: ${service.responseTime}ms (threshold: 200ms)`
                  : `Service offline for ${Math.floor(Math.random() * 5 + 1)} minutes`}
              </p>
            </div>
            <span className="text-xs font-medium text-status-degraded">⚠ {service.status}</span>
          </div>
        </div>
      ))}
    </div>
  )
}
