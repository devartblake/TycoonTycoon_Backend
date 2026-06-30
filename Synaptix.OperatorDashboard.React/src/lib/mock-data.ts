/**
 * Mock data generators for UI testing
 */

import type { User, UsersListResponse, SavedView } from '@/features/users/types'
import type { NotificationTemplate, NotificationChannel, ScheduledNotification, DeadLetterMessage } from '@/features/notifications/types'
import type { AntiCheatFlag, QueueStats } from '@/features/anti-cheat/types'

// ============ Users Mock Data ============

export function generateMockUsers(count: number = 20): User[] {
  const statuses: User['status'][] = ['active', 'suspended', 'banned', 'inactive']
  const users: User[] = []

  for (let i = 1; i <= count; i++) {
    users.push({
      id: `user_${i}`,
      email: `player${i}@synaptix.local`,
      status: statuses[Math.floor(Math.random() * statuses.length)],
      createdAt: new Date(Date.now() - Math.random() * 90 * 24 * 60 * 60 * 1000).toISOString(),
      lastActiveAt: Math.random() > 0.3 ? new Date(Date.now() - Math.random() * 7 * 24 * 60 * 60 * 1000).toISOString() : null,
      flaggedCount: Math.floor(Math.random() * 5),
      handle: `player_${i}`,
    })
  }

  return users
}

export function generateMockUsersList(): UsersListResponse {
  return {
    items: generateMockUsers(15),
    total: 247,
    offset: 0,
    limit: 50,
  }
}

export const MOCK_SAVED_VIEWS: SavedView[] = [
  {
    id: 'view_1',
    name: 'Banned This Week',
    filters: { status: 'banned' },
    createdAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
    updatedAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
  },
  {
    id: 'view_2',
    name: 'Flagged Accounts',
    filters: { flagged: true },
    createdAt: new Date(Date.now() - 14 * 24 * 60 * 60 * 1000).toISOString(),
    updatedAt: new Date(Date.now() - 14 * 24 * 60 * 60 * 1000).toISOString(),
  },
]

// ============ Notifications Mock Data ============

export const MOCK_TEMPLATES: NotificationTemplate[] = [
  {
    id: 'tpl_1',
    name: 'Welcome Email',
    subject: 'Welcome to Synaptix!',
    body: 'Hi {{playerName}}, welcome to Synaptix. Start your journey now!',
    channels: ['email'],
    variables: ['playerName'],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 'tpl_2',
    name: 'Daily Reward Notification',
    subject: 'Claim Your Daily Reward!',
    body: 'You have a {{rewardAmount}} coin reward waiting!',
    channels: ['push', 'email'],
    variables: ['rewardAmount'],
    createdAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
    updatedAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
  },
  {
    id: 'tpl_3',
    name: 'Level Up Alert',
    body: '🎉 Congratulations! You reached level {{level}}!',
    channels: ['push', 'sms'],
    variables: ['level'],
    createdAt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
    updatedAt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
  },
]

export const MOCK_CHANNELS: NotificationChannel[] = [
  {
    id: 'ch_1',
    type: 'email',
    name: 'Email Notifications',
    enabled: true,
    config: { provider: 'sendgrid', apiKey: '***' },
    createdAt: new Date(Date.now() - 90 * 24 * 60 * 60 * 1000).toISOString(),
  },
  {
    id: 'ch_2',
    type: 'push',
    name: 'Push Notifications',
    enabled: true,
    config: { provider: 'firebase', projectId: 'synaptix-prod' },
    createdAt: new Date(Date.now() - 90 * 24 * 60 * 60 * 1000).toISOString(),
  },
  {
    id: 'ch_3',
    type: 'sms',
    name: 'SMS Notifications',
    enabled: false,
    config: { provider: 'twilio', accountSid: '***' },
    createdAt: new Date(Date.now() - 60 * 24 * 60 * 60 * 1000).toISOString(),
  },
]

export const MOCK_SCHEDULES: ScheduledNotification[] = [
  {
    id: 'sch_1',
    templateId: 'tpl_1',
    templateName: 'Welcome Email',
    scheduledFor: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(),
    targetCount: 342,
    status: 'pending',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'sch_2',
    templateId: 'tpl_2',
    templateName: 'Daily Reward Notification',
    scheduledFor: new Date(Date.now() + 2 * 60 * 60 * 1000).toISOString(),
    targetCount: 8532,
    status: 'pending',
    createdAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
  },
  {
    id: 'sch_3',
    templateId: 'tpl_3',
    templateName: 'Level Up Alert',
    scheduledFor: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000).toISOString(),
    targetCount: 2100,
    status: 'completed',
    createdAt: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString(),
  },
]

export const MOCK_DEAD_LETTERS: DeadLetterMessage[] = [
  {
    id: 'dl_1',
    templateId: 'tpl_1',
    templateName: 'Welcome Email',
    channel: 'email',
    recipient: 'invalid@example.com',
    error: 'Invalid email address format',
    attemptCount: 3,
    createdAt: new Date(Date.now() - 5 * 60 * 60 * 1000).toISOString(),
    lastAttemptAt: new Date(Date.now() - 1 * 60 * 60 * 1000).toISOString(),
  },
  {
    id: 'dl_2',
    templateId: 'tpl_2',
    templateName: 'Daily Reward Notification',
    channel: 'push',
    recipient: 'device_token_xyz',
    error: 'Device token expired',
    attemptCount: 5,
    createdAt: new Date(Date.now() - 12 * 60 * 60 * 1000).toISOString(),
    lastAttemptAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
  },
]


// ============ Anti-Cheat Mock Data ============

export function generateMockAntiCheatFlag(id: string = 'flag_1'): AntiCheatFlag {
  const reasons = [
    'Rapid-fire suspicious timing',
    'Impossible accuracy pattern',
    'Bot-like response consistency',
    'Simultaneous multi-account activity',
    'Statistically impossible win rate',
  ]

  return {
    id,
    playerId: `player_${Math.floor(Math.random() * 1000)}`,
    playerEmail: `player${Math.floor(Math.random() * 1000)}@synaptix.local`,
    sessionId: `session_${Math.random().toString(36).substr(2, 9)}`,
    flagReason: reasons[Math.floor(Math.random() * reasons.length)],
    flagSeverity: ['low', 'medium', 'high', 'critical'][Math.floor(Math.random() * 4)] as any,
    sessionTime: new Date(Date.now() - Math.random() * 7 * 24 * 60 * 60 * 1000).toISOString(),
    telemetryData: {
      avgResponseTime: 250 + Math.random() * 500,
      responseTimeVariance: 50 + Math.random() * 300,
      accuracyRate: 85 + Math.random() * 15,
      suspiciousPatterns: [
        'Consistent 250ms response time (inhuman precision)',
        'Zero variance in difficulty perception',
        'Answer submitted before question fully loaded',
      ],
    },
    status: 'pending',
    createdAt: new Date(Date.now() - Math.random() * 24 * 60 * 60 * 1000).toISOString(),
  }
}

export const MOCK_ANTI_CHEAT_STATS: QueueStats = {
  pendingCount: 12,
  reviewedThisWeek: 89,
  completionRate: 0.92,
}
