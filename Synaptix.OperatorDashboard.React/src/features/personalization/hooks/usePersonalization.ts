import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import * as api from '../api'
import type { PlayerArchetype, RecommendationEngine, RecommendationControl } from '../types'

// Archetypes
export function useArchetypes() {
  return useQuery({
    queryKey: ['archetypes'],
    queryFn: () => api.getArchetypes(),
  })
}

export function useUpdateArchetype() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...data }: Partial<PlayerArchetype> & { id: string }) => api.updateArchetype(id, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['archetypes'] }),
  })
}

export function useDeleteArchetype() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.deleteArchetype(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['archetypes'] }),
  })
}

export function useRecalculateArchetype() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (archetypeId: string) => api.recalculateArchetypeMetrics(archetypeId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['archetypes'] }),
  })
}

// Recommendation Engines
export function useRecommendationEngines() {
  return useQuery({
    queryKey: ['recommendation-engines'],
    queryFn: () => api.getRecommendationEngines(),
  })
}

export function useToggleRecommendationEngine() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, enabled }: { id: string; enabled: boolean }) => api.toggleRecommendationEngine(id, enabled),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['recommendation-engines'] }),
  })
}

export function useUpdateRecommendationEngine() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...data }: Partial<RecommendationEngine> & { id: string }) => api.updateRecommendationEngine(id, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['recommendation-engines'] }),
  })
}

export function useResetRecommendationModel() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (engineId: string) => api.resetRecommendationModel(engineId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['recommendation-engines'] }),
  })
}

// Recommendation Controls
export function useRecommendationControls() {
  return useQuery({
    queryKey: ['recommendation-controls'],
    queryFn: () => api.getRecommendationControls(),
  })
}

export function useUpdateRecommendationControl() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...data }: Partial<RecommendationControl> & { id: string }) => api.updateRecommendationControl(id, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['recommendation-controls'] }),
  })
}

// Stats
export function usePersonalizationStats() {
  return useQuery({
    queryKey: ['personalization-stats'],
    queryFn: () => api.getPersonalizationStats(),
    staleTime: 1000 * 60 * 5, // 5 minutes
  })
}
