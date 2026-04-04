import { apiClient } from '../apiClient'
import type { NotificationHistoryResponse } from '../types/admin'

function toQuery(params: Record<string, unknown>): string {
  const parts: string[] = []

  for (const [key, val] of Object.entries(params)) {
    if (val !== undefined && val !== null && val !== '') {
      parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(val))}`)
    }
  }

  return parts.join('&')
}

export const auditService = {
  securityEvents(params: { from?: string; to?: string; status?: string; page?: number; pageSize?: number } = {}) {
    const qs = toQuery(params as Record<string, unknown>)

    return apiClient.get<NotificationHistoryResponse>(`/admin/audit/security${qs ? `?${qs}` : ''}`)
  }
}
