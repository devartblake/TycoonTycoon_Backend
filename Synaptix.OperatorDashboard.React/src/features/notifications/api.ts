/**
 * Notifications API — Django admin_notifications_client + AdminNotificationsEndpoints.
 *
 *   GET    /admin/notifications/channels
 *   PUT    /admin/notifications/channels/{key}
 *   POST   /admin/notifications/send
 *   POST   /admin/notifications/schedule
 *   GET    /admin/notifications/scheduled
 *   DELETE /admin/notifications/scheduled/{id}
 *   GET    /admin/notifications/templates
 *   POST   /admin/notifications/templates
 *   PATCH  /admin/notifications/templates/{id}
 *   DELETE /admin/notifications/templates/{id}
 *   GET    /admin/notifications/dead-letter
 *   POST   /admin/notifications/dead-letter/{id}/replay
 *   GET    /admin/notifications/history
 */

import { apiGet, apiPost, apiPut, apiPatch, apiDelete } from '@/lib/api-client'
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

type ChannelType = 'email' | 'push' | 'sms'

interface BackendTemplateDto {
  templateId: string
  name: string
  title: string
  body: string
  channelKey: string
  variables: string[]
  updatedAt: string
}

interface BackendChannelDto {
  key: string
  name: string
  description: string
  importance: string
  enabled: boolean
}

interface BackendScheduledItemDto {
  scheduleId: string
  title: string
  channelKey: string
  scheduledAt: string
  status: string
}

interface BackendListResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

function inferChannelType(channelKey: string): ChannelType {
  const key = channelKey.toLowerCase()
  if (key.includes('email') || key.includes('mail')) return 'email'
  if (key.includes('sms') || key.includes('text')) return 'sms'
  return 'push'
}

function toTemplate(dto: BackendTemplateDto): NotificationTemplate {
  return {
    id: dto.templateId,
    name: dto.name,
    subject: dto.title,
    body: dto.body,
    channels: [inferChannelType(dto.channelKey)],
    variables: dto.variables ?? [],
    createdAt: dto.updatedAt,
    updatedAt: dto.updatedAt,
  }
}

function toChannel(dto: BackendChannelDto): NotificationChannel {
  return {
    id: dto.key,
    type: inferChannelType(dto.key),
    name: dto.name,
    enabled: dto.enabled,
    config: { description: dto.description, importance: dto.importance },
    createdAt: '',
  }
}

function mapScheduleStatus(status: string): ScheduledNotification['status'] {
  switch (status) {
    case 'scheduled':
    case 'retry_pending':
      return 'pending'
    case 'processing':
      return 'in_progress'
    case 'sent':
    case 'completed':
      return 'completed'
    default:
      return 'failed'
  }
}

function toScheduled(dto: BackendScheduledItemDto): ScheduledNotification {
  return {
    id: dto.scheduleId,
    templateId: '',
    templateName: dto.title,
    scheduledFor: dto.scheduledAt,
    targetCount: 0,
    status: mapScheduleStatus(dto.status),
    createdAt: '',
  }
}

function toDeadLetter(dto: BackendScheduledItemDto): DeadLetterMessage {
  return {
    id: dto.scheduleId,
    templateId: '',
    templateName: dto.title,
    channel: inferChannelType(dto.channelKey),
    recipient: '',
    error: '',
    attemptCount: 0,
    createdAt: dto.scheduledAt,
    lastAttemptAt: dto.scheduledAt,
  }
}

async function fetchBackendTemplate(templateId: string): Promise<BackendTemplateDto | undefined> {
  const templates = await apiGet<BackendTemplateDto[]>('/admin/notifications/templates')
  return templates.find((t) => t.templateId === templateId)
}

export async function getTemplates(): Promise<NotificationTemplate[]> {
  if (getMockMode()) return mockApi.mockGetTemplates()
  const dtos = await apiGet<BackendTemplateDto[]>('/admin/notifications/templates')
  return dtos.map(toTemplate)
}

export async function getTemplate(templateId: string): Promise<NotificationTemplate> {
  if (getMockMode()) {
    const all = await mockApi.mockGetTemplates()
    return all.find((x) => x.id === templateId)!
  }
  const dto = await fetchBackendTemplate(templateId)
  if (!dto) throw new Error(`Template ${templateId} not found`)
  return toTemplate(dto)
}

export async function createTemplate(payload: CreateTemplatePayload): Promise<NotificationTemplate> {
  if (getMockMode()) return mockApi.mockCreateTemplate(payload)
  const dto = await apiPost<BackendTemplateDto>('/admin/notifications/templates', {
    name: payload.name,
    title: payload.subject ?? payload.name,
    body: payload.body,
    channelKey: payload.channels[0] ?? 'push',
    variables: [],
  })
  return toTemplate(dto)
}

export async function updateTemplate(
  templateId: string,
  payload: Partial<CreateTemplatePayload>
): Promise<NotificationTemplate> {
  if (getMockMode()) return mockApi.mockUpdateTemplate(templateId, payload)
  const current = await fetchBackendTemplate(templateId)
  if (!current) throw new Error(`Template ${templateId} not found`)
  const dto = await apiPatch<BackendTemplateDto>(`/admin/notifications/templates/${templateId}`, {
    name: payload.name ?? current.name,
    title: payload.subject ?? current.title,
    body: payload.body ?? current.body,
    channelKey: payload.channels?.[0] ?? current.channelKey,
    variables: current.variables ?? [],
  })
  return toTemplate(dto)
}

export async function deleteTemplate(templateId: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockDeleteTemplate(templateId)
  await apiDelete(`/admin/notifications/templates/${templateId}`)
  return { success: true }
}

export async function getChannels(): Promise<NotificationChannel[]> {
  if (getMockMode()) return mockApi.mockGetChannels()
  const dtos = await apiGet<BackendChannelDto[]>('/admin/notifications/channels')
  return dtos.map(toChannel)
}

export async function updateChannel(
  channelId: string,
  enabled: boolean,
  config?: Record<string, unknown>
): Promise<NotificationChannel> {
  if (getMockMode()) return mockApi.mockUpdateChannel(channelId, enabled, config)
  const channels = await apiGet<BackendChannelDto[]>('/admin/notifications/channels')
  const current = channels.find((c) => c.key === channelId)
  const dto = await apiPut<BackendChannelDto>(`/admin/notifications/channels/${channelId}`, {
    name: current?.name ?? channelId,
    description: (config?.description as string) ?? current?.description ?? '',
    importance: (config?.importance as string) ?? current?.importance ?? 'normal',
    enabled,
  })
  return toChannel(dto)
}

export async function getSchedules(): Promise<ScheduledNotification[]> {
  if (getMockMode()) return mockApi.mockGetSchedules()
  const res = await apiGet<BackendListResponse<BackendScheduledItemDto>>(
    '/admin/notifications/scheduled?page=1&pageSize=200'
  )
  return res.items.map(toScheduled)
}

export async function createSchedule(payload: {
  templateId: string
  scheduledFor: string
  targetFilter?: Record<string, unknown>
}): Promise<ScheduledNotification> {
  if (getMockMode()) return mockApi.mockCreateSchedule(payload)
  const template = await fetchBackendTemplate(payload.templateId)
  if (!template) throw new Error(`Template ${payload.templateId} not found`)
  const res = await apiPost<{ scheduleId: string }>('/admin/notifications/schedule', {
    title: template.title,
    body: template.body,
    channelKey: template.channelKey,
    scheduledAt: payload.scheduledFor,
    audience: payload.targetFilter ?? {},
  })
  return {
    id: res.scheduleId,
    templateId: payload.templateId,
    templateName: template.name,
    scheduledFor: payload.scheduledFor,
    targetCount: 0,
    status: 'pending',
    createdAt: new Date().toISOString(),
  }
}

export async function cancelSchedule(scheduleId: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockCancelSchedule(scheduleId)
  await apiDelete(`/admin/notifications/scheduled/${scheduleId}`)
  return { success: true }
}

export async function getDeadLetterMessages(): Promise<DeadLetterMessage[]> {
  if (getMockMode()) return mockApi.mockGetDeadLetterMessages()
  const res = await apiGet<BackendListResponse<BackendScheduledItemDto>>(
    '/admin/notifications/dead-letter?page=1&pageSize=200'
  )
  return res.items.map(toDeadLetter)
}

export async function retryDeadLetterMessage(messageId: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockRetryDeadLetter(messageId)
  await apiPost(`/admin/notifications/dead-letter/${messageId}/replay`, {})
  return { success: true }
}

export async function sendTestNotification(
  payload: TestSendPayload
): Promise<{ success: boolean; messageId: string }> {
  if (getMockMode()) return { success: true, messageId: `msg_${Date.now()}` }
  const template = await fetchBackendTemplate(payload.templateId)
  if (!template) throw new Error(`Template ${payload.templateId} not found`)
  const res = await apiPost<{ jobId: string }>('/admin/notifications/send', {
    title: template.title,
    body: template.body,
    channelKey: template.channelKey,
    audience: { recipient: payload.recipient },
    payload: payload.variables ?? {},
  })
  return { success: true, messageId: res.jobId }
}

/** Django get_notification_history parity */
export async function getNotificationHistory(
  page: number = 1,
  pageSize: number = 50
): Promise<BackendListResponse<Record<string, unknown>>> {
  if (getMockMode()) {
    return { items: [], page, pageSize, totalItems: 0, totalPages: 0 }
  }
  return apiGet(`/admin/notifications/history?page=${page}&pageSize=${pageSize}`)
}
