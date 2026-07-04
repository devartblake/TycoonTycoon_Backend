import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import * as api from '../api'

export function useSkills() {
  return useQuery({ queryKey: ['skills'], queryFn: () => api.getSkills() })
}

export function useSkillSeeds(skillId?: string) {
  return useQuery({ queryKey: ['skill-seeds', skillId], queryFn: () => api.getSkillSeeds(skillId) })
}

export function useSkillStats() {
  return useQuery({ queryKey: ['skill-stats'], queryFn: () => api.getSkillStats(), staleTime: 1000*60 })
}

export function useUpdateSkill() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({id, data}: any) => api.updateSkill(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['skills'] })
  })
}
