import { describe, it, expect, vi } from 'vitest'

// Force mock mode so the api returns in-memory samples (no network).
vi.mock('@/lib/api-config', () => ({ getMockMode: () => true }))

import * as api from './api'

describe('payments api (mock mode)', () => {
  it('lists checkout attempts', async () => {
    const res = await api.listPayments()
    expect(res.items.length).toBeGreaterThan(0)
    expect(res.total).toBe(res.items.length)
  })

  it('filters attempts by provider', async () => {
    const res = await api.listPayments({ provider: 'paypal' })
    expect(res.items.length).toBeGreaterThan(0)
    expect(res.items.every((a) => a.provider === 'paypal')).toBe(true)
  })

  it('filters attempts by status', async () => {
    const res = await api.listPayments({ status: 'Created' })
    expect(res.items.every((a) => a.status === 'Created')).toBe(true)
  })

  it('returns attempt detail with its issues', async () => {
    const detail = await api.getPayment('33333333-3333-3333-3333-333333333333')
    expect(detail.attempt.id).toBe('33333333-3333-3333-3333-333333333333')
    expect(detail.issues.every((i) => i.paymentCheckoutAttemptId === detail.attempt.id)).toBe(true)
  })

  it('lists only open issues when resolved=false', async () => {
    const res = await api.listIssues({ resolved: false })
    expect(res.items.every((i) => i.resolvedAtUtc === null)).toBe(true)
  })

  it('treats a blank amount as a full refund', async () => {
    const full = await api.refundPayment('id-1', 'duplicate charge')
    expect(full.isFullRefund).toBe(true)

    const partial = await api.refundPayment('id-1', 'partial', 1.0)
    expect(partial.isFullRefund).toBe(false)
  })

  it('marks a resolved issue with notes', async () => {
    const resolved = await api.resolveIssue('44444444-4444-4444-4444-444444444444', 'investigated')
    expect(resolved.resolvedAtUtc).not.toBeNull()
    expect(resolved.resolutionNotes).toBe('investigated')
  })
})
