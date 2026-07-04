import { apiGet } from '@/lib/api-client';
export async function getMatches(playerId, limit = 50) {
    const url = playerId ? '/admin/matches?player=' + playerId + '&limit=' + limit : '/admin/matches?limit=' + limit;
    return apiGet(url);
}
export async function getMatchStats(playerId) {
    return apiGet('/admin/matches/' + playerId + '/stats');
}
export async function getMatchReplays(playerId) {
    return apiGet('/admin/matches/' + playerId + '/replays');
}
