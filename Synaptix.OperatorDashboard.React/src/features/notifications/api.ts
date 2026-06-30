/**
 * Notifications API client
 */

import { apiGet, apiPost, apiPut } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type {
  NotificationTemplate,
  NotificationChannel,
  ScheduledNotification,
  DeadLetterMessage,
  TestSendPayload,
  CreateTemplatePayload,
} from './types'

// Templates
export async function getTemplates(): Promise<NotificationTemplate[]> {
  if (getMockMode()) return mockApi.mockGetTemplates()
  return apiGet('/admin/notifications/templates')
}

export async function getTemplate(templateId: string): Promise<NotificationTemplate> {
  if (getMockMode()) return mockApi.mockGetTemplates().then(t => t.find(x => x.id === templateId)!) as Promise<NotificationTemplate>
  return apiGet(`/admin/notifications/templates/${templateId}`)
}

export async function createTemplate(payload: CreateTemplatePayload): Promise<NotificationTemplate> {
  if (getMockMode()) return mockApi.mockCreateTemplate(payload)
  return apiPost('/admin/notifications/templates', payload)
}

export async function updateTemplate(
  templateId: string,
  payload: Partial<CreateTemplatePayload>
): Promise<NotificationTemplate> {
  if (getMockMode()) return mockApi.mockUpdateTemplate(templateId, payload)
  return apiPut(`/admin/notifications/templates/${templateId}`, payload)
}

export async function deleteTemplate(templateId: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockDeleteTemplate(templateId)
  return apiPost(`/admin/notifications/templates/${templateId}/delete`, {})
}

// Channels
export async function getChannels(): Promise<NotificationChannel[]> {
  if (getMockMode()) return mockApi.mockGetChannels()
  return apiGet('/admin/notifications/channels')
}

export async function updateChannel(
  channelId: string,
  enabled: boolean,
  config?: Record<string, unknown>
): Promise<NotificationChannel> {
  if (getMockMode()) return mockApi.mockUpdateChannel(channelId, enabled, config)
  return apiPut(`/admin/notifications/channels/${channelId}`, { enabled, config })
}

// Schedules
export async function getSchedules(): Promise<ScheduledNotification[]> {
  if (getMockMode()) return mockApi.mockGetSchedules()
  return apiGet('/admin/notifications/schedules')
}

export async function createSchedule(payload: {
  templateId: string
  scheduledFor: string
  targetFilter?: Record<string, unknown>
}): Promise<ScheduledNotification> {
  if (getMockMode()) return mockApi.mockCreateSchedule(payload)
  return apiPost('/admin/notifications/schedules', payload)
}

export async function cancelSchedule(scheduleId: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockCancelSchedule(scheduleId)
  return apiPost(`/admin/notifications/schedules/${scheduleId}/cancel`, {})
}

// Dead-letter
export async function getDeadLetterMessages(): Promise<DeadLetterMessage[]> {
  if (getMockMode()) return mockApi.mockGetDeadLetterMessages()
  return apiGet('/admin/notifications/dead-letter')
}

export async function retryDeadLetterMessage(messageId: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockRetryDeadLetter(messageId)
  return apiPost(`/admin/notifications/dead-letter/${messageId}/retry`, {})
}

// Test send
export async function sendTestNotification(payload: TestSendPayload): Promise<{ success: boolean; messageId: string }> {
  if (getMockMode()) return { success: true, messageId: `msg_${Date.now()}` }
  return apiPost('/admin/notifications/test-send', payload)
}
