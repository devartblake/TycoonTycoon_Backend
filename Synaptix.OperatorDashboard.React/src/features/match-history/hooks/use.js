import { useQuery } from '@tanstack/react-query';
import * as api from '../api';
export function useMatches(playerId) {
    return useQuery({
        queryKey: ['matches', playerId],
        queryFn: () => api.getMatches(playerId),
        staleTime: 1000 * 60 * 5
    });
}
export function useMatchStats(playerId) {
    return useQuery({
        queryKey: ['match-stats', playerId],
        queryFn: () => api.getMatchStats(playerId),
        staleTime: 1000 * 60 * 5
    });
}
export function useMatchReplays(playerId) {
    return useQuery({
        queryKey: ['match-replays', playerId],
        queryFn: () => api.getMatchReplays(playerId)
    });
}
