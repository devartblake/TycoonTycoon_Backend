/**
 * Anti-cheat API client
 */

import { apiGet, apiPost } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type { AntiCheatFlag, QueueStats, VerdictPayload, VerdictResponse } from './types'

export async function getQueueStats(): Promise<QueueStats> {
  if (getMockMode()) return mockApi.mockGetQueueStats()
  return apiGet('/admin/anti-cheat/stats')
}

export async function getNextFlag(): Promise<AntiCheatFlag> {
  if (getMockMode()) return mockApi.mockGetNextFlag()
  return apiGet('/admin/anti-cheat/queue?status=pending&limit=1')
}

export async function getFlagDetail(flagId: string): Promise<AntiCheatFlag> {
  if (getMockMode()) return mockApi.mockGetFlagDetail(flagId)
  return apiGet(`/admin/anti-cheat/flags/${flagId}`)
}

export async function submitVerdict(payload: VerdictPayload): Promise<VerdictResponse> {
  if (getMockMode()) return mockApi.mockSubmitVerdict(payload)
  return apiPost(`/admin/anti-cheat/flags/${payload.flagId}/verdict`, {
    verdict: payload.verdict,
    notes: payload.notes,
  })
}
