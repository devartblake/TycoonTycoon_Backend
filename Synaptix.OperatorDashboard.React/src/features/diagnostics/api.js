import { apiGet, apiPost, apiPut } from '@/lib/api-client';
export async function getProbeRecords() {
    return apiGet('/admin/diagnostics/probes');
}
export async function getProbeHistory(probeId, limit = 100) {
    return apiGet(`/admin/diagnostics/probes/${probeId}/history?limit=${limit}`);
}
export async function getProbeLogs(probeId, limit = 100) {
    const url = probeId ? `/admin/diagnostics/logs/${probeId}?limit=${limit}` : `/admin/diagnostics/logs?limit=${limit}`;
    return apiGet(url);
}
export async function getDiagnosticMetrics() {
    return apiGet('/admin/diagnostics/metrics');
}
export async function runProbeNow(probeId) {
    return apiPost(`/admin/diagnostics/probes/${probeId}/run`, {});
}
export async function updateProbe(probeId, data) {
    return apiPut(`/admin/diagnostics/probes/${probeId}`, data);
}
export async function enableProbe(probeId) {
    return apiPost(`/admin/diagnostics/probes/${probeId}/enable`, {});
}
export async function disableProbe(probeId) {
    return apiPost(`/admin/diagnostics/probes/${probeId}/disable`, {});
}
export async function exportLogs(startDate, endDate) {
    const response = await fetch(`/admin/diagnostics/logs/export?start=${startDate}&end=${endDate}`, { headers: { Authorization: `Bearer ${localStorage.getItem('token') || ''}` } });
    return response.blob();
}
