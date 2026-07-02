/**
 * useEconomy hook - manage player economy and transactions
 */
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import * as api from '../api';
export function usePlayerEconomy(playerId) {
    return useQuery({
        queryKey: ['playerEconomy', playerId],
        queryFn: () => api.getPlayerEconomy(playerId),
        enabled: !!playerId,
        staleTime: 1000 * 60, // 1 minute
    });
}
export function usePlayerTransactions(playerId, filters, offset = 0, limit = 50) {
    return useQuery({
        queryKey: ['playerTransactions', playerId, filters, offset, limit],
        queryFn: () => api.getPlayerTransactions(playerId, filters, offset, limit),
        enabled: !!playerId,
        staleTime: 1000 * 60, // 1 minute
    });
}
export function useAdjustBalance() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (adjustment) => api.adjustBalance(adjustment),
        onSuccess: (_, adjustment) => {
            queryClient.invalidateQueries({ queryKey: ['playerEconomy', adjustment.playerId] });
            queryClient.invalidateQueries({ queryKey: ['playerTransactions', adjustment.playerId] });
        },
    });
}
export function useIssueRefund() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ playerId, transactionId, reason, note }) => api.issueRefund(playerId, transactionId, reason, note),
        onSuccess: (_, { playerId }) => {
            queryClient.invalidateQueries({ queryKey: ['playerEconomy', playerId] });
            queryClient.invalidateQueries({ queryKey: ['playerTransactions', playerId] });
        },
    });
}
export function useEconomyStats() {
    return useQuery({
        queryKey: ['economyStats'],
        queryFn: () => api.getEconomyStats(),
        staleTime: 1000 * 60 * 5, // 5 minutes
    });
}
export function useSearchPlayers(query, limit = 20) {
    return useQuery({
        queryKey: ['playerSearch', query, limit],
        queryFn: () => api.searchPlayers(query, limit),
        enabled: query.length > 0,
        staleTime: 1000 * 60, // 1 minute
    });
}
