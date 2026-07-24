/**
 * Payments feature types — mirror AdminPaymentsEndpoints DTOs (camelCase wire form).
 *
 *   GET  /admin/payments                              (search checkout attempts)
 *   GET  /admin/payments/{id}                         (attempt + its issues)
 *   POST /admin/payments/{id}/reconcile
 *   POST /admin/payments/{id}/retry-fulfillment
 *   POST /admin/payments/{id}/refund                  body: { reason, amount? }
 *   GET  /admin/payment-reconciliation/issues         (list issues)
 *   POST /admin/payment-reconciliation/issues/{id}/resolve  body: { notes? }
 */

export type PaymentProvider = 'paypal' | 'stripe'

// PaymentCheckoutStatus (backend enum serialized as its name).
export type PaymentStatus = 'Created' | 'Captured' | 'Failed' | 'Expired' | 'Refunded'

// PaymentReconciliationCategory (serialized as its name).
export type ReconciliationCategory =
  | 'ProviderCapturedFulfillmentMissing'
  | 'AmountMismatch'
  | 'CurrencyMismatch'

export interface PaymentAttempt {
  id: string
  playerId: string
  provider: string
  sku: string
  quantity: number
  expectedAmount: number
  currency: string
  providerRef: string
  providerCaptureRef: string | null
  status: string
  playerTransactionId: string | null
  createdAtUtc: string
  resolvedAtUtc: string | null
}

export interface PaymentIssue {
  id: string
  category: string
  provider: string
  providerRef: string
  paymentCheckoutAttemptId: string | null
  playerId: string | null
  expectedAmount: number | null
  actualAmount: number | null
  details: string
  createdAtUtc: string
  resolvedAtUtc: string | null
  resolvedBy: string | null
  resolutionNotes: string | null
}

export interface PaymentListResponse {
  page: number
  pageSize: number
  total: number
  items: PaymentAttempt[]
}

export interface PaymentDetail {
  attempt: PaymentAttempt
  issues: PaymentIssue[]
}

export interface PaymentIssueListResponse {
  page: number
  pageSize: number
  total: number
  items: PaymentIssue[]
}

export interface PaymentListFilter {
  provider?: string
  status?: string
  playerId?: string
  page?: number
  pageSize?: number
}

export interface IssueListFilter {
  resolved?: boolean
  page?: number
  pageSize?: number
}

export interface ReconcileResponse {
  attemptId: string
  status: string
  issueRaised: boolean
}

export interface RetryFulfillmentResponse {
  attemptId: string
  playerTransactionId: string
  status: string
}

export interface RefundResponse {
  attemptId: string
  refundId: string
  refundStatus: string
  isFullRefund: boolean
  refundTransactionId: string
}
