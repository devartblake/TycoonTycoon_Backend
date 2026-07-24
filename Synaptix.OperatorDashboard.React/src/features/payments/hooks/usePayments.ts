/**
 * usePayments hooks — react-query wrappers over the payments API.
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as api from '../api'
import type { IssueListFilter, PaymentListFilter } from '../types'

export function usePayments(filter: PaymentListFilter) {
  return useQuery({
    queryKey: ['payments', filter],
    queryFn: () => api.listPayments(filter),
    staleTime: 1000 * 30,
  })
}

export function usePaymentDetail(id: string | null) {
  return useQuery({
    queryKey: ['payment', id],
    queryFn: () => api.getPayment(id as string),
    enabled: !!id,
    staleTime: 1000 * 30,
  })
}

export function usePaymentIssues(filter: IssueListFilter) {
  return useQuery({
    queryKey: ['paymentIssues', filter],
    queryFn: () => api.listIssues(filter),
    staleTime: 1000 * 30,
  })
}

function invalidatePayments(queryClient: ReturnType<typeof useQueryClient>) {
  queryClient.invalidateQueries({ queryKey: ['payments'] })
  queryClient.invalidateQueries({ queryKey: ['payment'] })
  queryClient.invalidateQueries({ queryKey: ['paymentIssues'] })
}

export function useReconcilePayment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.reconcilePayment(id),
    onSuccess: () => invalidatePayments(queryClient),
  })
}

export function useRetryFulfillment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.retryFulfillment(id),
    onSuccess: () => invalidatePayments(queryClient),
  })
}

export function useRefundPayment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, reason, amount }: { id: string; reason: string; amount?: number }) =>
      api.refundPayment(id, reason, amount),
    onSuccess: () => invalidatePayments(queryClient),
  })
}

export function useResolveIssue() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, notes }: { id: string; notes?: string }) => api.resolveIssue(id, notes),
    onSuccess: () => invalidatePayments(queryClient),
  })
}
