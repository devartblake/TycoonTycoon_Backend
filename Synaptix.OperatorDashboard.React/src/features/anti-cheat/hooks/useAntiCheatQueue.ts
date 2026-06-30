/**
 * useAntiCheatQueue hook - manage anti-cheat review queue
 */

import { useState, useEffect } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as api from '../api'
import type { AntiCheatFlag, VerdictPayload, VerdictResponse } from '../types'

export function useQueueStats() {
  return useQuery({
    queryKey: ['antiCheatStats'],
    queryFn: () => api.getQueueStats(),
    staleTime: 1000 * 60, // 1 minute
  })
}

export function useCurrentFlag() {
  return useQuery({
    queryKey: ['currentAntiCheatFlag'],
    queryFn: () => api.getNextFlag(),
    staleTime: 0, // Always fresh when needed
  })
}

export function useAntiCheatFlag(flagId: string) {
  return useQuery({
    queryKey: ['antiCheatFlag', flagId],
    queryFn: () => api.getFlagDetail(flagId),
    enabled: !!flagId,
    staleTime: 1000 * 60 * 5, // 5 minutes
  })
}

export function useSubmitVerdict() {
  const queryClient = useQueryClient()
  const [nextFlagId, setNextFlagId] = useState<string | undefined>()

  const mutation = useMutation({
    mutationFn: (payload: VerdictPayload) => api.submitVerdict(payload),
    onSuccess: (response: VerdictResponse) => {
      // Invalidate current flag and stats
      queryClient.invalidateQueries({ queryKey: ['currentAntiCheatFlag'] })
      queryClient.invalidateQueries({ queryKey: ['antiCheatStats'] })

      // Store next flag ID for UI to use
      if (response.nextFlagId) {
        setNextFlagId(response.nextFlagId)
      }
    },
  })

  return {
    ...mutation,
    nextFlagId,
    resetNextFlagId: () => setNextFlagId(undefined),
  }
}
