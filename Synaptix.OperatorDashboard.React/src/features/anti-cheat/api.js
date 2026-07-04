/**
 * Anti-cheat API client
 *
 * Reconciled to the real backend route surface under /admin/anti-cheat
 * (Synaptix.Backend.Api/Features/AdminAntiCheat). The backend exposes a flags
 * list and a review action; the dashboard's queue/stats/single-flag views are
 * derived from the flags list. Functions keep their existing return types.
 *
 * Known fidelity gaps (backend does not expose these fields; see #423):
 *   - AntiCheatFlag.playerEmail / sessionTime / telemetryData are best-effort
 *     placeholders (backend stores ruleKey/message/severity only).
 *   - QueueStats.reviewedThisWeek / completionRate are not exposed and default
 *     to 0; single-flag detail is resolved from the list (first 100).
 */
import { apiGet, apiPut } from '@/lib/api-client';
import { getMockMode } from '@/lib/api-config';
import * as mockApi from '@/lib/mock-api-client';
function mapSeverity(severity) {
    switch (severity) {
        case 0:
            return 'low';
        case 1:
            return 'medium';
        case 2:
            return 'high';
        case 3:
            return 'critical';
        default:
            return 'medium';
    }
}
function toFlag(dto) {
    return {
        id: dto.id,
        playerId: dto.playerId,
        playerEmail: '',
        sessionId: dto.matchId ?? '',
        flagReason: dto.ruleKey,
        flagSeverity: mapSeverity(dto.severity),
        sessionTime: '',
        telemetryData: {
            avgResponseTime: 0,
            responseTimeVariance: 0,
            accuracyRate: 0,
            suspiciousPatterns: dto.message ? [dto.message] : [],
        },
        status: dto.reviewedAtUtc ? 'reviewed' : 'pending',
        createdAt: dto.createdAtUtc,
    };
}
export async function getQueueStats() {
    if (getMockMode())
        return mockApi.mockGetQueueStats();
    // Backend has no /stats; derive the pending count from the unreviewed flags.
    const res = await apiGet('/admin/anti-cheat/flags?unreviewedOnly=true&page=1&pageSize=1');
    return { pendingCount: res.total, reviewedThisWeek: 0, completionRate: 0 };
}
export async function getNextFlag() {
    if (getMockMode())
        return mockApi.mockGetNextFlag();
    const res = await apiGet('/admin/anti-cheat/flags?unreviewedOnly=true&page=1&pageSize=1');
    if (res.items.length === 0)
        throw new Error('No pending anti-cheat flags.');
    return toFlag(res.items[0]);
}
export async function getFlagDetail(flagId) {
    if (getMockMode())
        return mockApi.mockGetFlagDetail(flagId);
    // Backend has no single-flag GET; resolve from the list.
    const res = await apiGet('/admin/anti-cheat/flags?page=1&pageSize=100');
    const dto = res.items.find((f) => f.id === flagId);
    if (!dto)
        throw new Error(`Anti-cheat flag ${flagId} not found`);
    return toFlag(dto);
}
export async function submitVerdict(payload) {
    if (getMockMode())
        return mockApi.mockSubmitVerdict(payload);
    // Map the verdict onto the backend review action.
    await apiPut(`/admin/anti-cheat/flags/${payload.flagId}/review`, {
        reviewedBy: 'operator',
        note: payload.notes ? `[${payload.verdict}] ${payload.notes}` : `[${payload.verdict}]`,
    });
    return { success: true };
}
