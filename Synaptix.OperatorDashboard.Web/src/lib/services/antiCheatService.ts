import { apiClient } from '../apiClient'
import type {
  AntiCheatFlag,
  AntiCheatSummary,
  ReviewAntiCheatFlagRequest,
  PlayerRiskRow,
  PartyAntiCheatFlag,
  PaginatedResponse
} from '../types/admin'

function toQuery(params: Record<string, unknown>): string {
  const parts: string[] = []

  for (const [key, val] of Object.entries(params)) {
    if (val !== undefined && val !== null && val !== '') {
      parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(val))}`)
    }
  }

  return parts.join('&')
}

export const antiCheatService = {
  flags(params: { unreviewedOnly?: boolean; severity?: number; playerId?: string; page?: number; pageSize?: number } = {}) {
    const qs = toQuery(params as Record<string, unknown>)

    return apiClient.get<PaginatedResponse<AntiCheatFlag>>(`/admin/anti-cheat/flags${qs ? `?${qs}` : ''}`)
  },

  reviewFlag(flagId: string, req: ReviewAntiCheatFlagRequest) {
    return apiClient.put<void>(`/admin/anti-cheat/flags/${flagId}/review`, req)
  },

  summary(windowHours = 24) {
    return apiClient.get<AntiCheatSummary>(`/admin/anti-cheat/summary?windowHours=${windowHours}`)
  },

  riskPlayers(params: { page?: number; pageSize?: number } = {}) {
    const qs = toQuery(params as Record<string, unknown>)

    return apiClient.get<PaginatedResponse<PlayerRiskRow>>(`/admin/anti-cheat/risk-players${qs ? `?${qs}` : ''}`)
  },

  // Party detection
  partyFlags(params: { unreviewedOnly?: boolean; page?: number; pageSize?: number } = {}) {
    const qs = toQuery(params as Record<string, unknown>)

    return apiClient.get<PaginatedResponse<PartyAntiCheatFlag>>(`/admin/party-detection/flags${qs ? `?${qs}` : ''}`)
  },

  reviewPartyFlag(flagId: string, reviewedBy?: string) {
    return apiClient.post<void>(`/admin/party-detection/flags/${flagId}/review`, { reviewedBy })
  }
}
