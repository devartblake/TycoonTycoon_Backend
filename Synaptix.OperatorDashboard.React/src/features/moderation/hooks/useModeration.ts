/**
 * useModeration hook - manage player moderation actions
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as api from '../api'

export function usePlayerModeration(playerId: string) {
  return useQuery({
    queryKey: ['playerModeration', playerId],
    queryFn: () => api.getPlayerModeration(playerId),
    enabled: !!playerId,
    staleTime: 1000 * 60, // 1 minute
  })
}

export function useBanPlayer() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ playerId, reason, notes }: { playerId: string; reason: string; notes?: string }) =>
      api.banPlayer(playerId, reason, notes),
    onSuccess: (_, { playerId }) => {
      queryClient.invalidateQueries({ queryKey: ['playerModeration', playerId] })
    },
  })
}

export function useUnbanPlayer() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ playerId, reason }: { playerId: string; reason: string }) =>
      api.unbanPlayer(playerId, reason),
    onSuccess: (_, { playerId }) => {
      queryClient.invalidateQueries({ queryKey: ['playerModeration', playerId] })
    },
  })
}

export function useSuspendPlayer() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ playerId, durationHours, reason, notes }: { playerId: string; durationHours: number; reason: string; notes?: string }) =>
      api.suspendPlayer(playerId, durationHours, reason, notes),
    onSuccess: (_, { playerId }) => {
      queryClient.invalidateQueries({ queryKey: ['playerModeration', playerId] })
    },
  })
}

export function useUnsuspendPlayer() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ playerId, reason }: { playerId: string; reason: string }) =>
      api.unsuspendPlayer(playerId, reason),
    onSuccess: (_, { playerId }) => {
      queryClient.invalidateQueries({ queryKey: ['playerModeration', playerId] })
    },
  })
}

export function useWarnPlayer() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ playerId, reason, notes }: { playerId: string; reason: string; notes?: string }) =>
      api.warnPlayer(playerId, reason, notes),
    onSuccess: (_, { playerId }) => {
      queryClient.invalidateQueries({ queryKey: ['playerModeration', playerId] })
    },
  })
}

export function useAddModeratorNote() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ playerId, note }: { playerId: string; note: string }) =>
      api.addModeratorNote(playerId, note),
    onSuccess: (_, { playerId }) => {
      queryClient.invalidateQueries({ queryKey: ['playerModeration', playerId] })
    },
  })
}
