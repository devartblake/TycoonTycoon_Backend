/**
 * useOperations hook - manage seasons and game events
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as api from '../api'
import type { SeasonFilter, EventFilter, LifecycleAction } from '../types'

export function useSeasons(filters?: SeasonFilter, offset: number = 0, limit: number = 50) {
  return useQuery({
    queryKey: ['seasons', filters, offset, limit],
    queryFn: () => api.getSeasons(filters, offset, limit),
    staleTime: 1000 * 60, // 1 minute
  })
}

export function useGameEvents(filters?: EventFilter, offset: number = 0, limit: number = 50) {
  return useQuery({
    queryKey: ['gameEvents', filters, offset, limit],
    queryFn: () => api.getGameEvents(filters, offset, limit),
    staleTime: 1000 * 60, // 1 minute
  })
}

export function usePerformLifecycleAction() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (action: LifecycleAction) => api.performLifecycleAction(action),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['seasons'] })
      queryClient.invalidateQueries({ queryKey: ['gameEvents'] })
      queryClient.invalidateQueries({ queryKey: ['operationsStats'] })
    },
  })
}

export function useOperationsStats() {
  return useQuery({
    queryKey: ['operationsStats'],
    queryFn: () => api.getOperationsStats(),
    staleTime: 1000 * 60 * 5, // 5 minutes
  })
}

export function useSeason(seasonId: string) {
  return useQuery({
    queryKey: ['season', seasonId],
    queryFn: () => api.getSeason(seasonId),
    enabled: !!seasonId,
    staleTime: 1000 * 60, // 1 minute
  })
}

export function useEvent(eventId: string) {
  return useQuery({
    queryKey: ['event', eventId],
    queryFn: () => api.getEvent(eventId),
    enabled: !!eventId,
    staleTime: 1000 * 60, // 1 minute
  })
}
