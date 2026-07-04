/**
 * Configuration API client
 * Handles feature flags, admin ACL, and system settings
 */
import { apiGet, apiPost, apiPut, apiPatch, apiDelete } from '@/lib/api-client';
// ── Feature Flags ────────────────────────────────────────────────────────────
export async function getFeatureFlags(offset = 0, limit = 50) {
    return apiGet(`/admin/config/feature-flags?offset=${offset}&limit=${limit}`);
}
export async function getFeatureFlag(id) {
    return apiGet(`/admin/config/feature-flags/${id}`);
}
export async function createFeatureFlag(flag) {
    return apiPost('/admin/config/feature-flags', flag);
}
export async function updateFeatureFlag(id, flag) {
    return apiPut(`/admin/config/feature-flags/${id}`, flag);
}
export async function toggleFeatureFlag(id, enabled) {
    return apiPatch(`/admin/config/feature-flags/${id}/toggle`, { enabled });
}
export async function deleteFeatureFlag(id) {
    await apiDelete(`/admin/config/feature-flags/${id}`);
    return { success: true };
}
// ── Admin ACL ────────────────────────────────────────────────────────────────
export async function getAdminACL(offset = 0, limit = 50) {
    return apiGet(`/admin/config/admin-acl?offset=${offset}&limit=${limit}`);
}
export async function getAdminACLEntry(id) {
    return apiGet(`/admin/config/admin-acl/${id}`);
}
export async function createAdminACL(acl) {
    return apiPost('/admin/config/admin-acl', acl);
}
export async function updateAdminACL(id, acl) {
    return apiPut(`/admin/config/admin-acl/${id}`, acl);
}
export async function deleteAdminACL(id) {
    await apiDelete(`/admin/config/admin-acl/${id}`);
    return { success: true };
}
// ── System Configuration ─────────────────────────────────────────────────────
export async function getSystemConfig() {
    return apiGet('/admin/config/system');
}
export async function updateSystemConfig(config) {
    return apiPut('/admin/config/system', config);
}
export async function setMaintenanceMode(enabled, message) {
    await apiPatch('/admin/config/system/maintenance', { enabled, message });
    return { success: true };
}
// ── Stats ────────────────────────────────────────────────────────────────────
export async function getConfigStats() {
    return apiGet('/admin/config/stats');
}
