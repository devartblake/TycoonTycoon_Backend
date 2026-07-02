/**
 * useEconomy hook - manage player economy and transactions
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as api from '../api'
import type { TransactionFilter, BalanceAdjustment } from '../types'

export function usePlayerEconomy(playerId: string) {
  return useQuery({
    queryKey: ['playerEconomy', playerId],
    queryFn: () => api.getPlayerEconomy(playerId),
    enabled: !!playerId,
    staleTime: 1000 * 60, // 1 minute
  })
}

export function usePlayerTransactions(playerId: string, filters?: TransactionFilter, offset: number = 0, limit: number = 50) {
  return useQuery({
    queryKey: ['playerTransactions', playerId, filters, offset, limit],
    queryFn: () => api.getPlayerTransactions(playerId, filters, offset, limit),
    enabled: !!playerId,
    staleTime: 1000 * 60, // 1 minute
  })
}

export function useAdjustBalance() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (adjustment: BalanceAdjustment) => api.adjustBalance(adjustment),
    onSuccess: (_, adjustment) => {
      queryClient.invalidateQueries({ queryKey: ['playerEconomy', adjustment.playerId] })
      queryClient.invalidateQueries({ queryKey: ['playerTransactions', adjustment.playerId] })
    },
  })
}

export function useIssueRefund() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ playerId, transactionId, reason, note }: { playerId: string; transactionId: string; reason: string; note?: string }) =>
      api.issueRefund(playerId, transactionId, reason, note),
    onSuccess: (_, { playerId }) => {
      queryClient.invalidateQueries({ queryKey: ['playerEconomy', playerId] })
      queryClient.invalidateQueries({ queryKey: ['playerTransactions', playerId] })
    },
  })
}

export function useEconomyStats() {
  return useQuery({
    queryKey: ['economyStats'],
    queryFn: () => api.getEconomyStats(),
    staleTime: 1000 * 60 * 5, // 5 minutes
  })
}

export function useSearchPlayers(query: string, limit: number = 20) {
  return useQuery({
    queryKey: ['playerSearch', query, limit],
    queryFn: () => api.searchPlayers(query, limit),
    enabled: query.length > 0,
    staleTime: 1000 * 60, // 1 minute
  })
}
