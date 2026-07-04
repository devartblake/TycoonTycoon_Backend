import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import * as api from '../api'
import type { InstallerConfig } from '../types'

// Installation Status
export function useInstallationStatus() {
  return useQuery({
    queryKey: ['installation-status'],
    queryFn: () => api.getInstallationStatus(),
    refetchInterval: 2000, // Refresh every 2 seconds during installation
  })
}

export function useStartInstallation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (config: InstallerConfig) => api.startInstallation(config),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['installation-status'] }),
  })
}

export function usePauseInstallation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => api.pauseInstallation(),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['installation-status'] }),
  })
}

export function useResumeInstallation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => api.resumeInstallation(),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['installation-status'] }),
  })
}

export function useResetInstallation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => api.resetInstallation(),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['installation-status'] }),
  })
}

// Bundle Management
export function useAvailableBundles() {
  return useQuery({
    queryKey: ['available-bundles'],
    queryFn: () => api.getAvailableBundles(),
    staleTime: 1000 * 60 * 5, // 5 minutes
  })
}

export function useUploadBundle() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ file, onProgress }: { file: File; onProgress?: (progress: number) => void }) =>
      api.uploadBundle(file, onProgress),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['available-bundles'] }),
  })
}

export function useValidateBundle() {
  return useMutation({
    mutationFn: (bundleId: string) => api.validateBundle(bundleId),
  })
}

export function useDeployBundle() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (bundleId: string) => api.deployBundle(bundleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['installation-status'] })
      queryClient.invalidateQueries({ queryKey: ['available-bundles'] })
    },
  })
}

export function useDeleteBundle() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (bundleId: string) => api.deleteBundle(bundleId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['available-bundles'] }),
  })
}

// Configuration
export function useInstallerConfig() {
  return useQuery({
    queryKey: ['installer-config'],
    queryFn: () => api.getInstallerConfig(),
  })
}

export function useUpdateInstallerConfig() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (config: Partial<InstallerConfig>) => api.updateInstallerConfig(config),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['installer-config'] }),
  })
}

// Environment & Health
export function useValidateEnvironment() {
  return useMutation({
    mutationFn: () => api.validateEnvironment(),
  })
}

export function useBackendHealth() {
  return useQuery({
    queryKey: ['backend-health'],
    queryFn: () => api.getBackendHealth(),
    refetchInterval: 5000, // Refresh every 5 seconds
  })
}

export function useBackendVersion() {
  return useQuery({
    queryKey: ['backend-version'],
    queryFn: () => api.getBackendVersion(),
    staleTime: 1000 * 60 * 60, // 1 hour
  })
}

export function useRestartBackend() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => api.restartBackend(),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['backend-health'] }),
  })
}

// Logs
export function useInstallationLogs(stepId?: string) {
  return useQuery({
    queryKey: ['installation-logs', stepId],
    queryFn: () => api.getInstallationLogs(stepId),
    refetchInterval: 1000, // Refresh every second
  })
}

export function useExportLogs() {
  return useMutation({
    mutationFn: () => api.exportInstallationLogs(),
  })
}
