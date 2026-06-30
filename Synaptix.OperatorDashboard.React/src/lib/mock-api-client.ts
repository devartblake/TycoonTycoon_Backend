/**
 * Mock API client for UI testing without backend
 */

import type { User, UsersListResponse, SavedView } from '@/features/users/types'
import type { NotificationTemplate, NotificationChannel, ScheduledNotification, DeadLetterMessage } from '@/features/notifications/types'
import type { AntiCheatFlag, QueueStats } from '@/features/anti-cheat/types'
import {
  generateMockUsers,
  generateMockUsersList,
  MOCK_SAVED_VIEWS,
  MOCK_TEMPLATES,
  MOCK_CHANNELS,
  MOCK_SCHEDULES,
  MOCK_DEAD_LETTERS,
  generateMockAntiCheatFlag,
  MOCK_ANTI_CHEAT_STATS,
} from './mock-data'

// Simulate network delay
const MOCK_DELAY = 300

function delay(ms: number = MOCK_DELAY) {
  return new Promise((resolve) => setTimeout(resolve, ms))
}

// ============ Users Mock API ============

export async function mockGetUsers(_filters?: any): Promise<UsersListResponse> {
  await delay()
  return generateMockUsersList()
}

export async function mockGetUserDetail(_userId: string): Promise<User> {
  await delay()
  const users = generateMockUsers(1)
  return { ...users[0], id: _userId }
}

export async function mockBanUser(_userId: string, _reason?: string): Promise<{ success: boolean }> {
  await delay()
  return { success: true }
}

export async function mockUnbanUser(_userId: string): Promise<{ success: boolean }> {
  await delay()
  return { success: true }
}

export async function mockGetSavedViews(): Promise<SavedView[]> {
  await delay()
  return MOCK_SAVED_VIEWS
}

export async function mockCreateSavedView(name: string, filters: any): Promise<SavedView> {
  await delay()
  return {
    id: `view_${Date.now()}`,
    name,
    filters,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  }
}

export async function mockDeleteSavedView(_viewId: string): Promise<{ success: boolean }> {
  await delay()
  return { success: true }
}

// ============ Notifications Mock API ============

export async function mockGetTemplates(): Promise<NotificationTemplate[]> {
  await delay()
  return MOCK_TEMPLATES
}

export async function mockCreateTemplate(payload: any): Promise<NotificationTemplate> {
  await delay()
  return {
    id: `tpl_${Date.now()}`,
    ...payload,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  }
}

export async function mockUpdateTemplate(templateId: string, payload: any): Promise<NotificationTemplate> {
  await delay()
  const template = MOCK_TEMPLATES.find((t) => t.id === templateId)
  return {
    ...template,
    ...payload,
    id: templateId,
    updatedAt: new Date().toISOString(),
  } as NotificationTemplate
}

export async function mockDeleteTemplate(_templateId: string): Promise<{ success: boolean }> {
  await delay()
  return { success: true }
}

export async function mockGetChannels(): Promise<NotificationChannel[]> {
  await delay()
  return MOCK_CHANNELS
}

export async function mockUpdateChannel(
  channelId: string,
  enabled: boolean,
  config?: any
): Promise<NotificationChannel> {
  await delay()
  const channel = MOCK_CHANNELS.find((c) => c.id === channelId)
  return {
    ...channel,
    enabled,
    config: config || channel?.config,
  } as NotificationChannel
}

export async function mockGetSchedules(): Promise<ScheduledNotification[]> {
  await delay()
  return MOCK_SCHEDULES
}

export async function mockCreateSchedule(payload: any): Promise<ScheduledNotification> {
  await delay()
  return {
    id: `sch_${Date.now()}`,
    ...payload,
    status: 'pending',
    createdAt: new Date().toISOString(),
  }
}

export async function mockCancelSchedule(_scheduleId: string): Promise<{ success: boolean }> {
  await delay()
  return { success: true }
}

export async function mockGetDeadLetterMessages(): Promise<DeadLetterMessage[]> {
  await delay()
  return MOCK_DEAD_LETTERS
}

export async function mockRetryDeadLetter(_messageId: string): Promise<{ success: boolean }> {
  await delay()
  return { success: true }
}

// ============ Anti-Cheat Mock API ============

export async function mockGetQueueStats(): Promise<QueueStats> {
  await delay()
  return MOCK_ANTI_CHEAT_STATS
}

export async function mockGetNextFlag(): Promise<AntiCheatFlag> {
  await delay()
  return generateMockAntiCheatFlag()
}

export async function mockGetFlagDetail(flagId: string): Promise<AntiCheatFlag> {
  await delay()
  return generateMockAntiCheatFlag(flagId)
}

export async function mockSubmitVerdict(_payload: any): Promise<{ success: boolean; nextFlagId?: string }> {
  await delay()
  // 80% of the time, return a next flag ID
  return {
    success: true,
    nextFlagId: Math.random() > 0.2 ? `flag_${Date.now()}` : undefined,
  }
}
