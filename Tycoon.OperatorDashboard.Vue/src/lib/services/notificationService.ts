import { apiClient } from '../apiClient'
import type {
  NotificationChannel,
  NotificationHistoryResponse,
  NotificationScheduleResponse,
  NotificationScheduledListResponse,
  SendNotificationRequest,
  SendNotificationResponse,
} from '../types/admin'

function toQuery(params: Record<string, unknown>): string {
  const parts: string[] = []

  for (const [key, val] of Object.entries(params)) {
    if (val !== undefined && val !== null && val !== '')
      parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(val))}`)
  }

  return parts.join('&')
}

export const notificationService = {
  channels() {
    return apiClient.get<NotificationChannel[]>('/admin/notifications/channels')
  },

  send(req: SendNotificationRequest) {
    return apiClient.post<SendNotificationResponse>('/admin/notifications/send', req)
  },

  history(params: { from?: string; to?: string; channelKey?: string; status?: string; page?: number; pageSize?: number } = {}) {
    const qs = toQuery(params as Record<string, unknown>)

    return apiClient.get<NotificationHistoryResponse>(`/admin/notifications/history${qs ? `?${qs}` : ''}`)
  },

  deadLetter(params: { page?: number; pageSize?: number } = {}) {
    const qs = toQuery(params as Record<string, unknown>)

    return apiClient.get<NotificationScheduledListResponse>(`/admin/notifications/dead-letter${qs ? `?${qs}` : ''}`)
  },

  replay(scheduleId: string) {
    return apiClient.post<NotificationScheduleResponse>(`/admin/notifications/dead-letter/${scheduleId}/replay`)
  },

  scheduled(params: { page?: number; pageSize?: number } = {}) {
    const qs = toQuery(params as Record<string, unknown>)

    return apiClient.get<NotificationScheduledListResponse>(`/admin/notifications/scheduled${qs ? `?${qs}` : ''}`)
  },

  cancelScheduled(scheduleId: string) {
    return apiClient.delete<void>(`/admin/notifications/scheduled/${scheduleId}`)
  },
}
