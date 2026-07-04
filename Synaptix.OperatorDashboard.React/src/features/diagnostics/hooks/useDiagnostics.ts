import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import * as api from '../api'

export function useProbeRecords() {
  return useQuery({
    queryKey: ['probe-records'],
    queryFn: () => api.getProbeRecords(),
    refetchInterval: 5000,
  })
}

export function useProbeHistory(probeId: string) {
  return useQuery({
    queryKey: ['probe-history', probeId],
    queryFn: () => api.getProbeHistory(probeId),
    refetchInterval: 3000,
  })
}

export function useProbeLogs(probeId?: string) {
  return useQuery({
    queryKey: ['probe-logs', probeId],
    queryFn: () => api.getProbeLogs(probeId),
    refetchInterval: 2000,
  })
}

export function useDiagnosticMetrics() {
  return useQuery({
    queryKey: ['diagnostic-metrics'],
    queryFn: () => api.getDiagnosticMetrics(),
    refetchInterval: 5000,
  })
}

export function useRunProbeNow() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (probeId: string) => api.runProbeNow(probeId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['probe-records'] })
      queryClient.invalidateQueries({ queryKey: ['probe-logs'] })
    },
  })
}

export function useUpdateProbe() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ probeId, data }: { probeId: string; data: any }) => api.updateProbe(probeId, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['probe-records'] }),
  })
}

export function useEnableProbe() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (probeId: string) => api.enableProbe(probeId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['probe-records'] }),
  })
}

export function useDisableProbe() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (probeId: string) => api.disableProbe(probeId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['probe-records'] }),
  })
}
