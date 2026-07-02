/**
 * Anti-cheat API client
 */
import { apiGet, apiPost } from '@/lib/api-client';
import { getMockMode } from '@/lib/api-config';
import * as mockApi from '@/lib/mock-api-client';
export async function getQueueStats() {
    if (getMockMode())
        return mockApi.mockGetQueueStats();
    return apiGet('/admin/anti-cheat/stats');
}
export async function getNextFlag() {
    if (getMockMode())
        return mockApi.mockGetNextFlag();
    return apiGet('/admin/anti-cheat/queue?status=pending&limit=1');
}
export async function getFlagDetail(flagId) {
    if (getMockMode())
        return mockApi.mockGetFlagDetail(flagId);
    return apiGet(`/admin/anti-cheat/flags/${flagId}`);
}
export async function submitVerdict(payload) {
    if (getMockMode())
        return mockApi.mockSubmitVerdict(payload);
    return apiPost(`/admin/anti-cheat/flags/${payload.flagId}/verdict`, {
        verdict: payload.verdict,
        notes: payload.notes,
    });
}
