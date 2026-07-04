import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import * as api from '../api'
import type { FeatureFlag, AdminACL } from '../types'

// Feature Flags
export function useFeatureFlags() {
  return useQuery({
    queryKey: ['feature-flags'],
    queryFn: () => api.getFeatureFlags(),
  })
}

export function useToggleFeatureFlag() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, enabled }: { id: string; enabled: boolean }) => api.toggleFeatureFlag(id, enabled),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['feature-flags'] }),
  })
}

export function useUpdateFeatureFlag() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...data }: Partial<FeatureFlag> & { id: string }) => api.updateFeatureFlag(id, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['feature-flags'] }),
  })
}

export function useDeleteFeatureFlag() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.deleteFeatureFlag(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['feature-flags'] }),
  })
}

// Admin ACL
export function useAdminACL() {
  return useQuery({
    queryKey: ['admin-acl'],
    queryFn: () => api.getAdminACL(),
  })
}

export function useUpdateAdminACL() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...data }: Partial<AdminACL> & { id: string }) => api.updateAdminACL(id, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-acl'] }),
  })
}

export function useDeleteAdminACL() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.deleteAdminACL(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-acl'] }),
  })
}

// System Config
export function useSystemConfig() {
  return useQuery({
    queryKey: ['system-config'],
    queryFn: () => api.getSystemConfig(),
  })
}

export function useUpdateSystemConfig() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (config: any) => api.updateSystemConfig(config),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['system-config'] }),
  })
}

export function useSetMaintenanceMode() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ enabled, message }: { enabled: boolean; message?: string }) =>
      api.setMaintenanceMode(enabled, message),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['system-config'] })
    },
  })
}

// Stats
export function useConfigStats() {
  return useQuery({
    queryKey: ['config-stats'],
    queryFn: () => api.getConfigStats(),
    staleTime: 1000 * 60, // 1 minute
  })
}
