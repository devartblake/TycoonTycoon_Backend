/**
 * Users API client
 */
import { apiGet, apiPost } from '@/lib/api-client';
import { getMockMode } from '@/lib/api-config';
import * as mockApi from '@/lib/mock-api-client';
export async function getUsers(filters) {
    if (getMockMode())
        return mockApi.mockGetUsers(filters);
    const params = new URLSearchParams();
    if (filters.email)
        params.append('email', filters.email);
    if (filters.status)
        params.append('status', filters.status);
    if (filters.flagged !== undefined)
        params.append('flagged', String(filters.flagged));
    params.append('limit', String(filters.limit || 50));
    params.append('offset', String(filters.offset || 0));
    return apiGet(`/admin/users?${params.toString()}`);
}
export async function getUserDetail(userId) {
    if (getMockMode())
        return mockApi.mockGetUserDetail(userId);
    return apiGet(`/admin/users/${userId}`);
}
export async function banUser(userId, reason) {
    if (getMockMode())
        return mockApi.mockBanUser(userId, reason);
    return apiPost(`/admin/users/${userId}/ban`, { reason });
}
export async function unbanUser(userId) {
    if (getMockMode())
        return mockApi.mockUnbanUser(userId);
    return apiPost(`/admin/users/${userId}/unban`, {});
}
export async function suspendUser(userId, reason) {
    if (getMockMode())
        return mockApi.mockBanUser(userId, reason);
    return apiPost(`/admin/users/${userId}/suspend`, { reason });
}
export async function unsuspendUser(userId) {
    if (getMockMode())
        return mockApi.mockUnbanUser(userId);
    return apiPost(`/admin/users/${userId}/unsuspend`, {});
}
export async function bulkBanUsers(userIds, reason) {
    if (getMockMode())
        return { success: true, affected: userIds.length };
    return apiPost('/admin/users/bulk/ban', { userIds, reason });
}
export async function bulkUnbanUsers(userIds) {
    if (getMockMode())
        return { success: true, affected: userIds.length };
    return apiPost('/admin/users/bulk/unban', { userIds });
}
// Saved Views (stored in Django during transition, will move to .NET)
export async function getSavedViews() {
    if (getMockMode())
        return mockApi.mockGetSavedViews();
    return apiGet('/operator/saved-views');
}
export async function createSavedView(name, filters) {
    if (getMockMode())
        return mockApi.mockCreateSavedView(name, filters);
    return apiPost('/operator/saved-views', { name, filters });
}
export async function deleteSavedView(viewId) {
    if (getMockMode())
        return mockApi.mockDeleteSavedView(viewId);
    return apiPost(`/operator/saved-views/${viewId}/delete`, {});
}
