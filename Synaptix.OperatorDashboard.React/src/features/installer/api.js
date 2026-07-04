/**
 * Backend Installer API client
 * Handles installation, configuration, and backend health monitoring
 */
import { apiGet, apiPost, apiPut } from '@/lib/api-client';
// ── Installation Management ──────────────────────────────────────────────────
export async function getInstallationStatus() {
    return apiGet('/admin/installer/status');
}
export async function startInstallation(config) {
    return apiPost('/admin/installer/start', config);
}
export async function pauseInstallation() {
    return apiPost('/admin/installer/pause', {});
}
export async function resumeInstallation() {
    return apiPost('/admin/installer/resume', {});
}
export async function resetInstallation() {
    return apiPost('/admin/installer/reset', {});
}
export async function rollbackInstallation(stepId) {
    return apiPost(`/admin/installer/rollback/${stepId}`, {});
}
// ── Backend Bundle Management ────────────────────────────────────────────────
export async function getAvailableBundles() {
    return apiGet('/admin/installer/bundles');
}
export async function uploadBundle(file, onProgress) {
    const formData = new FormData();
    formData.append('file', file);
    return new Promise((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        if (onProgress) {
            xhr.upload.addEventListener('progress', (e) => {
                if (e.lengthComputable) {
                    const progress = (e.loaded / e.total) * 100;
                    onProgress(progress);
                }
            });
        }
        xhr.addEventListener('load', () => {
            if (xhr.status === 200) {
                resolve(JSON.parse(xhr.responseText));
            }
            else {
                reject(new Error(`Upload failed with status ${xhr.status}`));
            }
        });
        xhr.addEventListener('error', () => reject(new Error('Upload failed')));
        xhr.open('POST', '/admin/installer/bundles/upload');
        xhr.setRequestHeader('Authorization', `Bearer ${localStorage.getItem('token') || ''}`);
        xhr.send(formData);
    });
}
export async function validateBundle(bundleId) {
    return apiPost(`/admin/installer/bundles/${bundleId}/validate`, {});
}
export async function deployBundle(bundleId) {
    return apiPost(`/admin/installer/bundles/${bundleId}/deploy`, {});
}
export async function deleteBundle(bundleId) {
    return apiPost(`/admin/installer/bundles/${bundleId}/delete`, {});
}
// ── Configuration ────────────────────────────────────────────────────────────
export async function getInstallerConfig() {
    return apiGet('/admin/installer/config');
}
export async function updateInstallerConfig(config) {
    return apiPut('/admin/installer/config', config);
}
export async function getInstallationRequirements() {
    return apiGet('/admin/installer/requirements');
}
export async function validateEnvironment() {
    return apiPost('/admin/installer/validate-environment', {});
}
// ── Backend Health & Monitoring ──────────────────────────────────────────────
export async function getBackendHealth() {
    return apiGet('/admin/installer/health');
}
export async function getInstallationLogs(stepId) {
    const url = stepId ? `/admin/installer/logs/${stepId}` : '/admin/installer/logs';
    return apiGet(url);
}
export async function exportInstallationLogs() {
    const response = await fetch('/admin/installer/logs/export', {
        headers: {
            Authorization: `Bearer ${localStorage.getItem('token') || ''}`,
        },
    });
    return response.blob();
}
export async function getBackendVersion() {
    return apiGet('/admin/installer/version');
}
export async function restartBackend() {
    return apiPost('/admin/installer/restart', {});
}
