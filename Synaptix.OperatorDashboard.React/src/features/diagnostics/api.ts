import { apiGet, apiPost, apiPut } from '@/lib/api-client'
import type { ProbeLog, ProbeRecord, DiagnosticMetrics } from './types'

export async function getProbeRecords(): Promise<ProbeRecord[]> {
  return apiGet('/admin/diagnostics/probes')
}

export async function getProbeHistory(probeId: string, limit: number = 100): Promise<ProbeLog[]> {
  return apiGet(`/admin/diagnostics/probes/${probeId}/history?limit=${limit}`)
}

export async function getProbeLogs(probeId?: string, limit: number = 100): Promise<ProbeLog[]> {
  const url = probeId ? `/admin/diagnostics/logs/${probeId}?limit=${limit}` : `/admin/diagnostics/logs?limit=${limit}`
  return apiGet(url)
}

export async function getDiagnosticMetrics(): Promise<DiagnosticMetrics> {
  return apiGet('/admin/diagnostics/metrics')
}

export async function runProbeNow(probeId: string): Promise<ProbeLog> {
  return apiPost(`/admin/diagnostics/probes/${probeId}/run`, {})
}

export async function updateProbe(probeId: string, data: Partial<ProbeRecord>): Promise<ProbeRecord> {
  return apiPut(`/admin/diagnostics/probes/${probeId}`, data)
}

export async function enableProbe(probeId: string): Promise<{ success: boolean }> {
  return apiPost(`/admin/diagnostics/probes/${probeId}/enable`, {})
}

export async function disableProbe(probeId: string): Promise<{ success: boolean }> {
  return apiPost(`/admin/diagnostics/probes/${probeId}/disable`, {})
}

export async function exportLogs(startDate: string, endDate: string): Promise<Blob> {
  const response = await fetch(
    `/admin/diagnostics/logs/export?start=${startDate}&end=${endDate}`,
    { headers: { Authorization: `Bearer ${localStorage.getItem('token') || ''}` } }
  )
  return response.blob()
}
