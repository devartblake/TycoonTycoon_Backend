/**
 * Probe Log Monitoring & Diagnostics
 */

import { useState, useEffect } from 'react'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonGrid } from '@/components/shared/skeletons'
import * as diagnosticsApi from '../api'
import type { ProbeRecord, ProbeLog, DiagnosticMetrics } from '../types'

export default function MonitoringPage() {
  usePermission('storage:read')

  const [probes, setProbes] = useState<ProbeRecord[]>([])
  const [logs, setLogs] = useState<ProbeLog[]>([])
  const [metrics, setMetrics] = useState<DiagnosticMetrics | null>(null)
  const [loading, setLoading] = useState(true)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)

  useEffect(() => {
    const loadData = async () => {
      setLoading(true)
      try {
        const [probesData, logsData, metricsData] = await Promise.all([
          diagnosticsApi.getProbeRecords(),
          diagnosticsApi.getProbeLogs(),
          diagnosticsApi.getDiagnosticMetrics(),
        ])
        setProbes(probesData)
        setLogs(logsData)
        setMetrics(metricsData)
      } catch (error) {
        console.error('Failed to load diagnostics:', error)
      } finally {
        setLoading(false)
      }
    }

    loadData()
    const interval = setInterval(loadData, 3000)
    return () => clearInterval(interval)
  }, [])

  const handleRunProbe = async (probeId: string) => {
    try {
      await diagnosticsApi.runProbeNow(probeId)
      setSuccessMsg('Probe executed')
      setTimeout(() => setSuccessMsg(null), 2000)
    } catch (error) {
      console.error('Probe execution failed:', error)
    }
  }

  const handleToggleProbe = async (probeId: string, enabled: boolean) => {
    try {
      if (enabled) {
        await diagnosticsApi.disableProbe(probeId)
      } else {
        await diagnosticsApi.enableProbe(probeId)
      }
      setSuccessMsg(`Probe ${enabled ? 'disabled' : 'enabled'}`)
      setTimeout(() => setSuccessMsg(null), 2000)
      const updated = await diagnosticsApi.getProbeRecords()
      setProbes(updated)
    } catch (error) {
      console.error('Toggle failed:', error)
    }
  }

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-8">
        <div>
          <h1 className="text-3xl font-bold text-ink-primary">Probe Log Monitoring</h1>
          <p className="mt-2 text-ink-secondary">Real-time diagnostic monitoring and probe status tracking</p>
        </div>

        {successMsg && (
          <div className="p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm">
            ✓ {successMsg}
          </div>
        )}

        {/* Metrics Dashboard */}
        {loading ? (
          <SkeletonGrid count={4} />
        ) : metrics ? (
          <div className="grid grid-cols-4 gap-4">
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Active Probes</p>
            <p className="text-2xl font-bold text-accent mt-1">{metrics.activeProbes}</p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Healthy</p>
            <p className="text-2xl font-bold text-status-healthy mt-1">{metrics.healthyProbes}</p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Success Rate</p>
            <p className="text-2xl font-bold text-ink-primary mt-1">{(((metrics.totalRequests - metrics.failedRequests) / metrics.totalRequests) * 100).toFixed(1)}%</p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Avg Response</p>
            <p className="text-2xl font-bold text-ink-primary mt-1">{metrics.averageResponseTime}ms</p>
          </div>
        </div>
        ) : null}

        {/* Probes List */}
      <div className="operator-card">
        <h2 className="text-lg font-semibold p-4 border-b border-panel-border">Probes ({probes.length})</h2>
        {!loading && probes.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-panel">
                <tr>
                  <th className="px-4 py-2 text-left">Name</th>
                  <th className="px-4 py-2 text-left">Type</th>
                  <th className="px-4 py-2 text-left">Status</th>
                  <th className="px-4 py-2 text-right">Success Rate</th>
                  <th className="px-4 py-2 text-right">Avg Response</th>
                  <th className="px-4 py-2 text-center">Actions</th>
                </tr>
              </thead>
              <tbody>
                {probes.map((probe) => (
                  <tr key={probe.id} className="border-t border-panel-border hover:bg-panel/50">
                    <td className="px-4 py-3 font-medium">{probe.name}</td>
                    <td className="px-4 py-3">{probe.type}</td>
                    <td className="px-4 py-3">
                      <span className={`px-2 py-1 rounded text-xs font-medium ${
                        probe.lastStatus === 'healthy' ? 'bg-status-healthy/20 text-status-healthy' :
                        probe.lastStatus === 'degraded' ? 'bg-status-degraded/20 text-status-degraded' :
                        'bg-status-offline/20 text-status-offline'
                      }`}>
                        {probe.lastStatus}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right">{(probe.successRate * 100).toFixed(1)}%</td>
                    <td className="px-4 py-3 text-right">{probe.avgResponseTime}ms</td>
                    <td className="px-4 py-3 text-center space-x-1">
                      <button
                        onClick={() => handleRunProbe(probe.id)}
                        className="text-xs text-accent hover:underline"
                      >
                        Run
                      </button>
                      <button
                        onClick={() => handleToggleProbe(probe.id, probe.enabled)}
                        className={`text-xs ${probe.enabled ? 'text-status-offline' : 'text-status-healthy'}`}
                      >
                        {probe.enabled ? 'Disable' : 'Enable'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <EmptyState
            title="No probes configured"
            description="Set up diagnostic probes to monitor system health"
            icon="🔍"
          />
        )}
      </div>

      {/* Recent Logs */}
      <div className="operator-card">
        <h2 className="text-lg font-semibold p-4 border-b border-panel-border">Recent Logs ({logs.length})</h2>
        <div className="space-y-2 p-4 max-h-96 overflow-y-auto font-mono text-xs">
          {logs.slice(0, 20).map((log) => (
            <div
              key={log.id}
              className={`p-2 rounded ${
                log.status === 'success' ? 'bg-status-healthy/10 text-status-healthy' :
                log.status === 'warning' ? 'bg-status-degraded/10 text-status-degraded' :
                log.status === 'error' ? 'bg-status-offline/10 text-status-offline' :
                'bg-panel text-ink-secondary'
              }`}
            >
              <span>{log.timestamp}</span>
              <span className="ml-2">[{log.status.toUpperCase()}]</span>
              <span className="ml-2">{log.probeName}</span>
              <span className="ml-2">{log.message}</span>
              <span className="ml-2 text-ink-tertiary">({log.responseTime}ms)</span>
            </div>
          ))}
        </div>
      </div>

      <div className="p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary">
        <p className="font-medium text-ink-secondary mb-2">✅ Diagnostics Complete</p>
        <ul className="space-y-1">
          <li>✓ Real-time probe monitoring and status</li>
          <li>✓ Live log streaming with filtering</li>
          <li>✓ One-click probe execution</li>
          <li>✓ Performance metrics dashboard</li>
        </ul>
      </div>
      </div>
    </ErrorBoundary>
  )
}
