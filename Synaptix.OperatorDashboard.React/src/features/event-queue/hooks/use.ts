import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import * as api from '../api'

export function useQueuedEvents(status?: string) {
  return useQuery({
    queryKey: ['event-queue', status],
    queryFn: () => api.getQueuedEvents(status),
    refetchInterval: 2000
  })
}

export function useEventStats() {
  return useQuery({
    queryKey: ['event-stats'],
    queryFn: () => api.getEventStats(),
    refetchInterval: 3000
  })
}

export function useRetryEvent() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (eventId: string) => api.retryEvent(eventId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['event-queue'] })
  })
}

export function useClearFailedEvents() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () => api.clearFailedEvents(),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['event-queue'] })
  })
}
