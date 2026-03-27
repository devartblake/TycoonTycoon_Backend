import { apiClient } from '../apiClient'
import type {
  ModerationLogItem,
  ModerationProfile,
  PaginatedResponse,
  RunEscalationRequest,
  RunEscalationResponse,
  SetModerationStatusRequest,
} from '../types/admin'

function toQuery(params: Record<string, unknown>): string {
  const parts: string[] = []

  for (const [key, val] of Object.entries(params)) {
    if (val !== undefined && val !== null && val !== '')
      parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(val))}`)
  }

  return parts.join('&')
}

export const moderationService = {
  getProfile(playerId: string) {
    return apiClient.get<ModerationProfile>(`/admin/moderation/profile/${playerId}`)
  },

  setStatus(req: SetModerationStatusRequest) {
    return apiClient.post<void>('/admin/moderation/set-status', req)
  },

  logs(params: { playerId?: string; page?: number; pageSize?: number } = {}) {
    const qs = toQuery(params as Record<string, unknown>)

    return apiClient.get<PaginatedResponse<ModerationLogItem>>(`/admin/moderation/logs${qs ? `?${qs}` : ''}`)
  },

  runEscalation(req: RunEscalationRequest) {
    return apiClient.post<RunEscalationResponse>('/admin/moderation/escalation/run', req)
  },
}
