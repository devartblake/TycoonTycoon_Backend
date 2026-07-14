import FeatureUnavailable from '@/components/shared/feature-unavailable'

export default function DiagnosticsUnavailablePage() {
  return (
    <FeatureUnavailable
      title="Probe diagnostics disabled"
      reason="/admin/diagnostics/* routes are not part of Synaptix.Backend.Api. Use platform health endpoints and the monitoring stack instead."
      alternatives={[
        {
          label: 'API health',
          note: 'GET /healthz · GET /health/ready (or /health /alive depending on deploy)',
        },
        {
          label: 'Alpha alerts runbook',
          note: 'ops/runbooks/alpha-launch-alerts.md · Prometheus group alpha-launch',
        },
        {
          label: 'Dashboard home',
          href: '/dashboard',
          note: 'Service cards and health metrics on the main dashboard',
        },
      ]}
    />
  )
}
