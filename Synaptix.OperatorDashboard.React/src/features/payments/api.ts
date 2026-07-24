/**
 * Payments API — AdminPaymentsEndpoints (nested under /admin, ops-key + admin-role gated).
 * See ./types for the endpoint map. Mock mode returns small in-memory samples so the
 * dashboard is usable without a backend.
 */

import { apiGet, apiPost } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import type {
  IssueListFilter,
  PaymentDetail,
  PaymentIssue,
  PaymentIssueListResponse,
  PaymentListFilter,
  PaymentListResponse,
  ReconcileResponse,
  RefundResponse,
  RetryFulfillmentResponse,
} from './types'

// ── Mock data ───────────────────────────────────────────────────────────────

const MOCK_ATTEMPTS = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    playerId: '99999999-9999-9999-9999-999999999999',
    provider: 'paypal',
    sku: 'powerup:skip',
    quantity: 1,
    expectedAmount: 2.99,
    currency: 'USD',
    providerRef: 'PAYPAL-ORDER-ABC123',
    providerCaptureRef: 'CAP-XYZ789',
    status: 'Captured',
    playerTransactionId: '22222222-2222-2222-2222-222222222222',
    createdAtUtc: new Date(Date.now() - 3600_000).toISOString(),
    resolvedAtUtc: new Date(Date.now() - 3000_000).toISOString(),
  },
  {
    id: '33333333-3333-3333-3333-333333333333',
    playerId: '88888888-8888-8888-8888-888888888888',
    provider: 'stripe',
    sku: 'sub:premium:monthly',
    quantity: 1,
    expectedAmount: 4.99,
    currency: 'USD',
    providerRef: 'cs_test_456',
    providerCaptureRef: null,
    status: 'Created',
    playerTransactionId: null,
    createdAtUtc: new Date(Date.now() - 1800_000).toISOString(),
    resolvedAtUtc: null,
  },
]

const MOCK_ISSUES = [
  {
    id: '44444444-4444-4444-4444-444444444444',
    category: 'ProviderCapturedFulfillmentMissing',
    provider: 'paypal',
    providerRef: 'PAYPAL-ORDER-DEF456',
    paymentCheckoutAttemptId: '33333333-3333-3333-3333-333333333333',
    playerId: '88888888-8888-8888-8888-888888888888',
    expectedAmount: 4.99,
    actualAmount: 4.99,
    details: 'Provider reports captured but no PlayerTransaction exists.',
    createdAtUtc: new Date(Date.now() - 900_000).toISOString(),
    resolvedAtUtc: null,
    resolvedBy: null,
    resolutionNotes: null,
  },
]

// ── Endpoints ─────────────────────────────────────────────────────────────────

export async function listPayments(filter: PaymentListFilter = {}): Promise<PaymentListResponse> {
  if (getMockMode()) {
    let items = MOCK_ATTEMPTS
    if (filter.provider) items = items.filter((a) => a.provider === filter.provider)
    if (filter.status) items = items.filter((a) => a.status === filter.status)
    if (filter.playerId) items = items.filter((a) => a.playerId === filter.playerId)
    return { page: filter.page ?? 1, pageSize: filter.pageSize ?? 20, total: items.length, items }
  }
  return apiGet<PaymentListResponse>('/admin/payments', {
    params: {
      provider: filter.provider || undefined,
      status: filter.status || undefined,
      playerId: filter.playerId || undefined,
      page: filter.page ?? 1,
      pageSize: filter.pageSize ?? 20,
    },
  })
}

export async function getPayment(id: string): Promise<PaymentDetail> {
  if (getMockMode()) {
    const attempt = MOCK_ATTEMPTS.find((a) => a.id === id) ?? MOCK_ATTEMPTS[0]
    const issues = MOCK_ISSUES.filter((i) => i.paymentCheckoutAttemptId === id)
    return { attempt, issues }
  }
  return apiGet<PaymentDetail>(`/admin/payments/${id}`)
}

export async function reconcilePayment(id: string): Promise<ReconcileResponse> {
  if (getMockMode()) return { attemptId: id, status: 'Captured', issueRaised: false }
  return apiPost<ReconcileResponse>(`/admin/payments/${id}/reconcile`)
}

export async function retryFulfillment(id: string): Promise<RetryFulfillmentResponse> {
  if (getMockMode()) {
    return { attemptId: id, playerTransactionId: '22222222-2222-2222-2222-222222222222', status: 'Fulfilled' }
  }
  return apiPost<RetryFulfillmentResponse>(`/admin/payments/${id}/retry-fulfillment`)
}

export async function refundPayment(id: string, reason: string, amount?: number): Promise<RefundResponse> {
  if (getMockMode()) {
    return {
      attemptId: id,
      refundId: 'MOCK-REFUND-1',
      refundStatus: 'COMPLETED',
      isFullRefund: amount == null,
      refundTransactionId: '55555555-5555-5555-5555-555555555555',
    }
  }
  return apiPost<RefundResponse>(`/admin/payments/${id}/refund`, {
    reason,
    amount: amount ?? null,
  })
}

export async function listIssues(filter: IssueListFilter = {}): Promise<PaymentIssueListResponse> {
  if (getMockMode()) {
    let items = MOCK_ISSUES
    if (filter.resolved === true) items = items.filter((i) => i.resolvedAtUtc != null)
    if (filter.resolved === false) items = items.filter((i) => i.resolvedAtUtc == null)
    return { page: filter.page ?? 1, pageSize: filter.pageSize ?? 20, total: items.length, items }
  }
  return apiGet<PaymentIssueListResponse>('/admin/payment-reconciliation/issues', {
    params: {
      resolved: filter.resolved,
      page: filter.page ?? 1,
      pageSize: filter.pageSize ?? 20,
    },
  })
}

export async function resolveIssue(id: string, notes?: string): Promise<PaymentIssue> {
  if (getMockMode()) {
    const issue = MOCK_ISSUES.find((i) => i.id === id) ?? MOCK_ISSUES[0]
    return { ...issue, resolvedAtUtc: new Date().toISOString(), resolvedBy: 'mock-operator', resolutionNotes: notes ?? null }
  }
  return apiPost<PaymentIssue>(`/admin/payment-reconciliation/issues/${id}/resolve`, { notes: notes ?? null })
}
