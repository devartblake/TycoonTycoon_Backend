/**
 * Moderation API client
 */
import { apiGet, apiPost } from '@/lib/api-client';
import { getMockMode } from '@/lib/api-config';
import * as mockApi from '@/lib/mock-api-client';
export async function getPlayerModeration(playerId) {
    if (getMockMode())
        return mockApi.mockGetPlayerModeration(playerId);
    return apiGet(`/admin/moderation/players/${playerId}`);
}
export async function banPlayer(playerId, reason, notes) {
    if (getMockMode())
        return mockApi.mockBanPlayer(playerId, reason, notes);
    return apiPost(`/admin/moderation/players/${playerId}/ban`, { reason, notes });
}
export async function unbanPlayer(playerId, reason) {
    if (getMockMode())
        return mockApi.mockUnbanPlayer(playerId, reason);
    return apiPost(`/admin/moderation/players/${playerId}/unban`, { reason });
}
export async function suspendPlayer(playerId, durationHours, reason, notes) {
    if (getMockMode())
        return mockApi.mockSuspendPlayer(playerId, durationHours, reason, notes);
    return apiPost(`/admin/moderation/players/${playerId}/suspend`, { durationHours, reason, notes });
}
export async function unsuspendPlayer(playerId, reason) {
    if (getMockMode())
        return mockApi.mockUnsuspendPlayer(playerId, reason);
    return apiPost(`/admin/moderation/players/${playerId}/unsuspend`, { reason });
}
export async function warnPlayer(playerId, reason, notes) {
    if (getMockMode())
        return mockApi.mockWarnPlayer(playerId, reason, notes);
    return apiPost(`/admin/moderation/players/${playerId}/warn`, { reason, notes });
}
export async function addModeratorNote(playerId, note) {
    if (getMockMode())
        return mockApi.mockAddModeratorNote(playerId, note);
    return apiPost(`/admin/moderation/players/${playerId}/note`, { note });
}
