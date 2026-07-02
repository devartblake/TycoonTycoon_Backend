/**
 * Moderation API client
 */

import { apiGet, apiPost } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type { PlayerModeration, ModerationAction } from './types'

export async function getPlayerModeration(playerId: string): Promise<PlayerModeration> {
  if (getMockMode()) return mockApi.mockGetPlayerModeration(playerId)
  return apiGet(`/admin/moderation/players/${playerId}`)
}

export async function banPlayer(playerId: string, reason: string, notes?: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockBanPlayer(playerId, reason, notes)
  return apiPost(`/admin/moderation/players/${playerId}/ban`, { reason, notes })
}

export async function unbanPlayer(playerId: string, reason: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockUnbanPlayer(playerId, reason)
  return apiPost(`/admin/moderation/players/${playerId}/unban`, { reason })
}

export async function suspendPlayer(playerId: string, durationHours: number, reason: string, notes?: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockSuspendPlayer(playerId, durationHours, reason, notes)
  return apiPost(`/admin/moderation/players/${playerId}/suspend`, { durationHours, reason, notes })
}

export async function unsuspendPlayer(playerId: string, reason: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockUnsuspendPlayer(playerId, reason)
  return apiPost(`/admin/moderation/players/${playerId}/unsuspend`, { reason })
}

export async function warnPlayer(playerId: string, reason: string, notes?: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockWarnPlayer(playerId, reason, notes)
  return apiPost(`/admin/moderation/players/${playerId}/warn`, { reason, notes })
}

export async function addModeratorNote(playerId: string, note: string): Promise<ModerationAction> {
  if (getMockMode()) return mockApi.mockAddModeratorNote(playerId, note)
  return apiPost(`/admin/moderation/players/${playerId}/note`, { note })
}
