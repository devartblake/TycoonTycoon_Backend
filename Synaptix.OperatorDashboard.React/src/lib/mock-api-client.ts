/**
 * Mock API client for UI testing without backend
 */

import type { UsersListResponse, SavedView, UserDetail, UserActivityResponse } from '@/features/users/types'
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

export async function mockGetUserDetail(_userId: string): Promise<UserDetail> {
  await delay()
  const users = generateMockUsers(1)
  const base = { ...users[0], id: _userId }
  return {
    ...base,
    username: base.handle ?? base.email.split('@')[0],
    role: 'player',
    ageGroup: 'adult',
    totalGamesPlayed: 128,
    totalPoints: 5420,
    winRate: 0.54,
    isVerified: true,
    isBanned: base.status === 'banned',
  }
}

export async function mockGetUserActivity(_userId: string, page: number = 1, pageSize: number = 20): Promise<UserActivityResponse> {
  await delay()
  const types = ['login', 'match-completed', 'purchase', 'profile-updated']
  const items = Array.from({ length: pageSize }, (_, i) => ({
    id: `act-${page}-${i}`,
    type: types[i % types.length],
    description: `Mock ${types[i % types.length]} event`,
    createdAt: new Date(Date.now() - i * 3600_000).toISOString(),
  }))
  return { items, page, pageSize, totalItems: 60, totalPages: Math.ceil(60 / pageSize) }
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

// ============ Audit Mock API ============

export async function mockGetAuditEvents(_filters?: any, offset: number = 0, limit: number = 50): Promise<any> {
  await delay()
  const events = []
  const eventTypes = ['login', 'api_call', 'permission_change', 'data_export', 'deletion', 'configuration_change']
  const countries = ['US', 'UK', 'CA', 'DE', 'JP', 'AU', 'FR', 'SG']
  const cities: Record<string, string[]> = {
    US: ['New York', 'San Francisco', 'Los Angeles'],
    UK: ['London', 'Manchester'],
    CA: ['Toronto', 'Vancouver'],
    DE: ['Berlin', 'Munich'],
    JP: ['Tokyo', 'Osaka'],
    AU: ['Sydney', 'Melbourne'],
    FR: ['Paris', 'Lyon'],
    SG: ['Singapore'],
  }

  for (let i = 0; i < limit; i++) {
    const country = countries[Math.floor(Math.random() * countries.length)]
    const cityList = cities[country]
    const city = cityList[Math.floor(Math.random() * cityList.length)]

    events.push({
      id: `audit_${offset + i}`,
      eventType: eventTypes[Math.floor(Math.random() * eventTypes.length)],
      adminEmail: `admin${Math.floor(Math.random() * 50)}@synaptix.local`,
      adminId: `admin_${Math.floor(Math.random() * 100)}`,
      resourceType: ['user', 'flag', 'config', 'template'][Math.floor(Math.random() * 4)],
      resourceId: `resource_${Math.floor(Math.random() * 1000)}`,
      action: ['create', 'update', 'delete', 'read'][Math.floor(Math.random() * 4)],
      ipAddress: `${Math.floor(Math.random() * 256)}.${Math.floor(Math.random() * 256)}.${Math.floor(Math.random() * 256)}.${Math.floor(Math.random() * 256)}`,
      userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
      country,
      city,
      latitude: 40 + Math.random() * 40,
      longitude: -120 + Math.random() * 100,
      status: Math.random() > 0.1 ? 'success' : 'failure',
      failureReason: Math.random() > 0.1 ? undefined : 'Permission denied',
      timestamp: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString(),
    })
  }

  return {
    items: events,
    total: 500 + offset,
    offset,
    limit,
  }
}

export async function mockGetAuditStats(): Promise<any> {
  await delay()
  return {
    totalEvents: 2847,
    successRate: 0.96,
    uniqueAdmins: 12,
    uniqueIPs: 45,
  }
}

export async function mockGetIPLocations(_filters?: any): Promise<any> {
  await delay()
  const countries = ['US', 'UK', 'CA', 'DE', 'JP', 'AU', 'FR', 'SG']
  const cities: Record<string, { name: string; lat: number; lon: number }[]> = {
    US: [
      { name: 'New York', lat: 40.7128, lon: -74.006 },
      { name: 'San Francisco', lat: 37.7749, lon: -122.4194 },
      { name: 'Los Angeles', lat: 34.0522, lon: -118.2437 },
    ],
    UK: [
      { name: 'London', lat: 51.5074, lon: -0.1278 },
      { name: 'Manchester', lat: 53.4808, lon: -2.2426 },
    ],
    CA: [
      { name: 'Toronto', lat: 43.6532, lon: -79.3832 },
      { name: 'Vancouver', lat: 49.2827, lon: -123.1207 },
    ],
    DE: [
      { name: 'Berlin', lat: 52.52, lon: 13.405 },
      { name: 'Munich', lat: 48.1351, lon: 11.582 },
    ],
    JP: [
      { name: 'Tokyo', lat: 35.6762, lon: 139.6503 },
      { name: 'Osaka', lat: 34.6937, lon: 135.5023 },
    ],
    AU: [
      { name: 'Sydney', lat: -33.8688, lon: 151.2093 },
      { name: 'Melbourne', lat: -37.8136, lon: 144.9631 },
    ],
    FR: [
      { name: 'Paris', lat: 48.8566, lon: 2.3522 },
      { name: 'Lyon', lat: 45.764, lon: 4.8357 },
    ],
    SG: [
      { name: 'Singapore', lat: 1.3521, lon: 103.8198 },
    ],
  }

  const locations = []
  for (const country of countries) {
    const cityList = cities[country]
    const city = cityList[Math.floor(Math.random() * cityList.length)]
    locations.push({
      ip: `${Math.floor(Math.random() * 256)}.${Math.floor(Math.random() * 256)}.${Math.floor(Math.random() * 256)}.${Math.floor(Math.random() * 256)}`,
      country,
      city: city.name,
      latitude: city.lat,
      longitude: city.lon,
      eventCount: Math.floor(Math.random() * 50) + 5,
    })
  }

  return locations
}

export async function mockGetEventDetail(eventId: string): Promise<any> {
  await delay()
  return {
    id: eventId,
    eventType: 'api_call',
    adminEmail: 'admin@synaptix.local',
    adminId: 'admin_1',
    resourceType: 'user',
    resourceId: 'user_123',
    action: 'update',
    ipAddress: '192.168.1.1',
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
    country: 'US',
    city: 'New York',
    latitude: 40.7128,
    longitude: -74.006,
    status: 'success',
    timestamp: new Date().toISOString(),
    details: {
      changes: [
        { field: 'status', oldValue: 'active', newValue: 'suspended' },
        { field: 'flagCount', oldValue: 5, newValue: 6 },
      ],
    },
  }
}

// ============ Dashboard Mock API ============

export async function mockGetDashboardStats(): Promise<any> {
  await delay()
  return {
    services: [
      {
        id: 'api-gateway',
        name: 'api-gateway',
        displayName: 'API Gateway',
        status: 'healthy',
        uptime: 99.98,
        responseTime: 45,
        lastCheckedAt: new Date().toISOString(),
        nextCheckAt: new Date(Date.now() + 30000).toISOString(),
        description: 'Main API entry point',
        endpoint: 'https://api.synaptix.local',
      },
      {
        id: 'auth-service',
        name: 'auth-service',
        displayName: 'Authentication Service',
        status: 'healthy',
        uptime: 99.99,
        responseTime: 32,
        lastCheckedAt: new Date().toISOString(),
        nextCheckAt: new Date(Date.now() + 30000).toISOString(),
        description: 'JWT token provider',
        endpoint: 'https://auth.synaptix.local',
      },
      {
        id: 'user-service',
        name: 'user-service',
        displayName: 'User Service',
        status: 'healthy',
        uptime: 99.95,
        responseTime: 58,
        lastCheckedAt: new Date().toISOString(),
        nextCheckAt: new Date(Date.now() + 30000).toISOString(),
        description: 'User management',
        endpoint: 'https://users.synaptix.local',
      },
      {
        id: 'database',
        name: 'database',
        displayName: 'Primary Database',
        status: 'degraded',
        uptime: 98.5,
        responseTime: 250,
        lastCheckedAt: new Date().toISOString(),
        nextCheckAt: new Date(Date.now() + 30000).toISOString(),
        description: 'PostgreSQL cluster',
        endpoint: 'postgres.internal',
      },
      {
        id: 'cache',
        name: 'cache',
        displayName: 'Redis Cache',
        status: 'healthy',
        uptime: 99.97,
        responseTime: 8,
        lastCheckedAt: new Date().toISOString(),
        nextCheckAt: new Date(Date.now() + 30000).toISOString(),
        description: 'Distributed cache',
        endpoint: 'redis.internal',
      },
      {
        id: 'storage',
        name: 'storage',
        displayName: 'Object Storage',
        status: 'healthy',
        uptime: 99.92,
        responseTime: 120,
        lastCheckedAt: new Date().toISOString(),
        nextCheckAt: new Date(Date.now() + 30000).toISOString(),
        description: 'S3-compatible storage',
        endpoint: 's3.synaptix.local',
      },
    ],
    metrics: {
      apiGatewayRequests: 42580,
      activeConnections: 287,
      cpuUsage: 34.2,
      memoryUsage: 56.8,
      diskUsage: 42.1,
      avgResponseTime: 87,
      errorRate: 0.2,
    },
    lastUpdatedAt: new Date().toISOString(),
    checksPerformed: 2847,
    alertsActive: 1,
  }
}

export async function mockGetServiceHistory(serviceId: string, hours: number = 24): Promise<any> {
  await delay()
  const metrics = []
  const now = Date.now()
  const interval = (hours * 60 * 60 * 1000) / 24 // One data point per hour

  for (let i = 0; i < 24; i++) {
    const value = serviceId === 'database'
      ? 95 + Math.random() * 5 // Database: 95-100% uptime
      : 98 + Math.random() * 2 // Others: 98-100% uptime

    metrics.push({
      timestamp: new Date(now - (24 - i) * interval).toISOString(),
      value: Math.round(value * 100) / 100,
    })
  }

  return {
    serviceId,
    metrics,
  }
}

export async function mockGetAllServiceHistory(hours: number = 24): Promise<any> {
  await delay()
  const serviceIds = ['api-gateway', 'auth-service', 'user-service', 'database', 'cache', 'storage']
  const histories = await Promise.all(
    serviceIds.map((id) => mockGetServiceHistory(id, hours))
  )
  return histories
}

// ============ Moderation Mock API ============

export async function mockGetPlayerModeration(playerId: string): Promise<any> {
  await delay()
  const statuses: ('active' | 'suspended' | 'banned' | 'inactive')[] = ['active', 'suspended', 'banned', 'inactive']
  const status = statuses[Math.floor(Math.random() * statuses.length)]

  return {
    profile: {
      id: playerId,
      email: `player${playerId.slice(-3)}@synaptix.local`,
      handle: `player_${playerId}`,
      status,
      createdAt: new Date(Date.now() - Math.random() * 365 * 24 * 60 * 60 * 1000).toISOString(),
      lastActiveAt: status === 'banned' ? null : new Date(Date.now() - Math.random() * 7 * 24 * 60 * 60 * 1000).toISOString(),
      flagCount: Math.floor(Math.random() * 10),
      accountBalance: Math.floor(Math.random() * 50000),
      totalSpent: Math.floor(Math.random() * 100000),
      playtimeHours: Math.floor(Math.random() * 500),
      winRate: 40 + Math.random() * 50,
      trustScore: Math.max(0, 100 - Math.random() * 40),
    },
    actions: [
      {
        id: 'action_1',
        playerId,
        adminEmail: 'moderator@synaptix.local',
        action: status === 'banned' ? 'ban' : 'warn',
        reason: status === 'banned' ? 'Repeated cheating violations' : 'Suspicious behavior detected',
        notes: 'Flagged by anti-cheat system',
        duration: status === 'suspended' ? 7 * 24 * 60 * 60 * 1000 : undefined,
        expiresAt: status === 'suspended' ? new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString() : undefined,
        status: 'active',
        createdAt: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString(),
      },
    ],
    activity: [
      {
        id: 'activity_1',
        playerId,
        type: 'login',
        description: 'Player logged in',
        timestamp: new Date(Date.now() - Math.random() * 24 * 60 * 60 * 1000).toISOString(),
      },
      {
        id: 'activity_2',
        playerId,
        type: 'game_played',
        description: 'Completed arcade game session',
        metadata: { score: Math.floor(Math.random() * 10000), duration: Math.floor(Math.random() * 300) },
        timestamp: new Date(Date.now() - Math.random() * 48 * 60 * 60 * 1000).toISOString(),
      },
      {
        id: 'activity_3',
        playerId,
        type: 'purchase',
        description: 'Purchased store item',
        metadata: { amount: Math.floor(Math.random() * 5000) },
        timestamp: new Date(Date.now() - Math.random() * 72 * 60 * 60 * 1000).toISOString(),
      },
    ],
    stats: {
      totalWarnings: Math.floor(Math.random() * 5),
      totalBans: Math.floor(Math.random() * 2),
      lastAction: {
        id: 'action_1',
        playerId,
        adminEmail: 'moderator@synaptix.local',
        action: 'warn',
        reason: 'Suspicious behavior',
        status: 'active',
        createdAt: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString(),
      },
    },
  }
}

export async function mockBanPlayer(playerId: string, reason: string, notes?: string): Promise<any> {
  await delay()
  return {
    id: `action_${Date.now()}`,
    playerId,
    adminEmail: 'moderator@synaptix.local',
    action: 'ban',
    reason,
    notes,
    status: 'active',
    createdAt: new Date().toISOString(),
  }
}

export async function mockUnbanPlayer(playerId: string, reason: string): Promise<any> {
  await delay()
  return {
    id: `action_${Date.now()}`,
    playerId,
    adminEmail: 'moderator@synaptix.local',
    action: 'unban',
    reason,
    status: 'active',
    createdAt: new Date().toISOString(),
  }
}

export async function mockSuspendPlayer(playerId: string, durationHours: number, reason: string, notes?: string): Promise<any> {
  await delay()
  const expiresAt = new Date(Date.now() + durationHours * 60 * 60 * 1000)
  return {
    id: `action_${Date.now()}`,
    playerId,
    adminEmail: 'moderator@synaptix.local',
    action: 'suspend',
    reason,
    notes,
    duration: durationHours * 60 * 60 * 1000,
    expiresAt: expiresAt.toISOString(),
    status: 'active',
    createdAt: new Date().toISOString(),
  }
}

export async function mockUnsuspendPlayer(playerId: string, reason: string): Promise<any> {
  await delay()
  return {
    id: `action_${Date.now()}`,
    playerId,
    adminEmail: 'moderator@synaptix.local',
    action: 'unsuspend',
    reason,
    status: 'active',
    createdAt: new Date().toISOString(),
  }
}

export async function mockWarnPlayer(playerId: string, reason: string, notes?: string): Promise<any> {
  await delay()
  return {
    id: `action_${Date.now()}`,
    playerId,
    adminEmail: 'moderator@synaptix.local',
    action: 'warn',
    reason,
    notes,
    status: 'active',
    createdAt: new Date().toISOString(),
  }
}

export async function mockAddModeratorNote(playerId: string, note: string): Promise<any> {
  await delay()
  return {
    id: `action_${Date.now()}`,
    playerId,
    adminEmail: 'moderator@synaptix.local',
    action: 'note',
    reason: note,
    status: 'active',
    createdAt: new Date().toISOString(),
  }
}

// ============ Economy Mock API ============

export async function mockGetPlayerEconomy(playerId: string): Promise<any> {
  await delay()
  const balance = Math.floor(Math.random() * 100000)
  const earned = balance + Math.floor(Math.random() * 50000)
  const spent = Math.floor(Math.random() * 30000)

  return {
    playerId,
    email: `player${playerId.slice(-3)}@synaptix.local`,
    handle: `player_${playerId}`,
    currentBalance: balance,
    totalEarned: earned,
    totalSpent: spent,
    totalRefunded: Math.floor(Math.random() * 5000),
    lastTransactionAt: new Date(Date.now() - Math.random() * 7 * 24 * 60 * 60 * 1000).toISOString(),
    accountCreatedAt: new Date(Date.now() - Math.random() * 365 * 24 * 60 * 60 * 1000).toISOString(),
  }
}

export async function mockGetPlayerTransactions(playerId: string, _filters?: any, offset: number = 0, limit: number = 50): Promise<any> {
  await delay()
  const types: any[] = ['purchase', 'earn', 'refund', 'adjustment', 'reward', 'penalty']
  const transactions = []

  let balance = 50000
  for (let i = 0; i < limit; i++) {
    const type = types[Math.floor(Math.random() * types.length)]
    const amount = Math.floor(Math.random() * 10000) + 100
    const balanceBefore = balance
    balance += type === 'earn' || type === 'reward' ? amount : -amount

    transactions.push({
      id: `txn_${offset + i}`,
      playerId,
      type,
      amount,
      balanceBefore,
      balanceAfter: Math.max(0, balance),
      description: `${type} transaction`,
      status: Math.random() > 0.05 ? 'completed' : 'failed',
      createdAt: new Date(Date.now() - Math.random() * 90 * 24 * 60 * 60 * 1000).toISOString(),
    })
  }

  return {
    items: transactions,
    total: 200 + offset,
    offset,
    limit,
  }
}

export async function mockAdjustBalance(_adjustment: any): Promise<any> {
  await delay()
  const newBalance = Math.max(0, Math.floor(Math.random() * 100000))
  return {
    success: true,
    newBalance,
  }
}

export async function mockIssueRefund(_playerId: string, _transactionId: string, _reason: string, _note?: string): Promise<any> {
  await delay()
  return {
    success: true,
    refundAmount: Math.floor(Math.random() * 10000),
  }
}

export async function mockGetEconomyStats(): Promise<any> {
  await delay()
  return {
    totalPlayers: 5247,
    totalCurrency: 524700000,
    averageBalance: 100000,
    largestBalance: 5000000,
    smallestBalance: 0,
  }
}

export async function mockSearchPlayers(query: string, limit: number = 20): Promise<any> {
  await delay()
  const results = []
  for (let i = 0; i < Math.min(limit, 5); i++) {
    results.push({
      playerId: `player_${i}`,
      email: `${query.toLowerCase()}${i}@synaptix.local`,
      handle: `${query}_${i}`,
      currentBalance: Math.floor(Math.random() * 100000),
    })
  }
  return results
}

// ============ Content Mock API ============

const QUESTION_TEMPLATES = [
  {
    text: 'What is the capital of France?',
    category: 'Geography',
    answers: [
      { text: 'Paris', isCorrect: true },
      { text: 'London', isCorrect: false },
      { text: 'Berlin', isCorrect: false },
      { text: 'Madrid', isCorrect: false },
    ],
  },
  {
    text: 'Which planet is known as the Red Planet?',
    category: 'Science',
    answers: [
      { text: 'Venus', isCorrect: false },
      { text: 'Mars', isCorrect: true },
      { text: 'Jupiter', isCorrect: false },
      { text: 'Saturn', isCorrect: false },
    ],
  },
  {
    text: 'What is the largest ocean on Earth?',
    category: 'Geography',
    answers: [
      { text: 'Atlantic Ocean', isCorrect: false },
      { text: 'Indian Ocean', isCorrect: false },
      { text: 'Arctic Ocean', isCorrect: false },
      { text: 'Pacific Ocean', isCorrect: true },
    ],
  },
  {
    text: 'In what year did the Titanic sink?',
    category: 'History',
    answers: [
      { text: '1912', isCorrect: true },
      { text: '1910', isCorrect: false },
      { text: '1915', isCorrect: false },
      { text: '1920', isCorrect: false },
    ],
  },
  {
    text: 'What is the smallest prime number?',
    category: 'Mathematics',
    answers: [
      { text: '0', isCorrect: false },
      { text: '1', isCorrect: false },
      { text: '2', isCorrect: true },
      { text: '3', isCorrect: false },
    ],
  },
]

export async function mockGetQuestions(_filters?: any, offset: number = 0, limit: number = 50): Promise<any> {
  await delay()
  const difficulties: any[] = ['easy', 'medium', 'hard']
  const statuses: any[] = ['pending', 'approved', 'rejected']
  const questions = []

  for (let i = 0; i < limit; i++) {
    const template = QUESTION_TEMPLATES[Math.floor(Math.random() * QUESTION_TEMPLATES.length)]
    const status = statuses[Math.floor(Math.random() * statuses.length)]

    questions.push({
      id: `question_${offset + i}`,
      text: template.text,
      category: template.category,
      difficulty: difficulties[Math.floor(Math.random() * difficulties.length)],
      answers: template.answers.map((a, idx) => ({ id: `answer_${idx}`, text: a.text, isCorrect: a.isCorrect })),
      correctAnswerId: `answer_0`,
      explanation: 'This is the correct answer because...',
      source: `contributor_${Math.floor(Math.random() * 100)}`,
      status,
      rejectionReason: status === 'rejected' ? 'Incorrect answer key' : undefined,
      submittedBy: `user_${Math.floor(Math.random() * 1000)}`,
      submittedAt: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString(),
      reviewedBy: status !== 'pending' ? 'moderator@synaptix.local' : undefined,
      reviewedAt: status !== 'pending' ? new Date(Date.now() - Math.random() * 7 * 24 * 60 * 60 * 1000).toISOString() : undefined,
      tags: ['trivia', 'educational'],
    })
  }

  return {
    items: questions,
    total: 500 + offset,
    offset,
    limit,
  }
}

export async function mockGetQuestionDetail(questionId: string): Promise<any> {
  await delay()
  const template = QUESTION_TEMPLATES[Math.floor(Math.random() * QUESTION_TEMPLATES.length)]
  return {
    id: questionId,
    text: template.text,
    category: template.category,
    difficulty: ['easy', 'medium', 'hard'][Math.floor(Math.random() * 3)],
    answers: template.answers.map((a, idx) => ({ id: `answer_${idx}`, text: a.text, isCorrect: a.isCorrect })),
    correctAnswerId: 'answer_0',
    explanation: 'This is the correct answer because...',
    source: `contributor_123`,
    status: 'pending',
    submittedBy: 'user_999',
    submittedAt: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(),
    tags: ['trivia', 'educational'],
  }
}

export async function mockReviewQuestion(_review: any): Promise<any> {
  await delay()
  return {
    success: true,
    nextQuestionId: `question_${Date.now()}`,
  }
}

export async function mockBulkReviewQuestions(questionIds: string[], _verdict: string, _reason?: string): Promise<any> {
  await delay()
  return {
    success: true,
    reviewed: questionIds.length,
  }
}

export async function mockGetQuestionsStats(): Promise<any> {
  await delay()
  return {
    totalPending: 247,
    totalApproved: 5234,
    totalRejected: 156,
    approvalRate: 0.97,
    avgReviewTime: 8.5,
  }
}

export async function mockGetCategories(): Promise<any> {
  await delay()
  return ['Geography', 'Science', 'History', 'Mathematics', 'Literature', 'Sports', 'Technology', 'Arts']
}

// ============ Operations Mock API ============

export async function mockGetSeasons(_filters?: any, offset: number = 0, limit: number = 50): Promise<any> {
  await delay()
  const statuses: any[] = ['draft', 'scheduled', 'active', 'ended']
  const seasons = []

  for (let i = 0; i < limit; i++) {
    const status = statuses[Math.floor(Math.random() * statuses.length)]
    const startDate = new Date(Date.now() + Math.random() * 90 * 24 * 60 * 60 * 1000)
    const endDate = new Date(startDate.getTime() + 30 * 24 * 60 * 60 * 1000)

    seasons.push({
      id: `season_${offset + i}`,
      name: `Season ${offset + i + 1}`,
      description: 'An exciting new season with fresh challenges and rewards',
      status,
      number: offset + i + 1,
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      rewardPool: Math.floor(Math.random() * 1000000) + 100000,
      pointsMultiplier: 1 + Math.random() * 2,
      createdAt: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString(),
      createdBy: 'admin@synaptix.local',
      startedAt: status !== 'draft' ? new Date(Date.now() - Math.random() * 60 * 24 * 60 * 60 * 1000).toISOString() : undefined,
      endedAt: status === 'ended' ? new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString() : undefined,
    })
  }

  return {
    items: seasons,
    total: 100 + offset,
    offset,
    limit,
  }
}

export async function mockGetGameEvents(_filters?: any, offset: number = 0, limit: number = 50): Promise<any> {
  await delay()
  const statuses: any[] = ['draft', 'upcoming', 'active', 'ended', 'cancelled']
  const types: any[] = ['tournament', 'challenge', 'promotion', 'special']
  const events = []

  for (let i = 0; i < limit; i++) {
    const status = statuses[Math.floor(Math.random() * statuses.length)]
    const type = types[Math.floor(Math.random() * types.length)]
    const startDate = new Date(Date.now() + Math.random() * 60 * 24 * 60 * 60 * 1000)
    const endDate = new Date(startDate.getTime() + 7 * 24 * 60 * 60 * 1000)

    events.push({
      id: `event_${offset + i}`,
      name: `${type.charAt(0).toUpperCase() + type.slice(1)} ${offset + i + 1}`,
      description: 'An exciting event with great rewards for participants',
      type,
      status,
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      reward: Math.floor(Math.random() * 50000) + 5000,
      participantCount: Math.floor(Math.random() * 5000),
      maxParticipants: Math.floor(Math.random() * 10000) + 5000,
      createdAt: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString(),
      createdBy: 'admin@synaptix.local',
      openedAt: status !== 'draft' ? new Date(Date.now() - Math.random() * 60 * 24 * 60 * 60 * 1000).toISOString() : undefined,
      closedAt: (status === 'ended' || status === 'cancelled') ? new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString() : undefined,
    })
  }

  return {
    items: events,
    total: 200 + offset,
    offset,
    limit,
  }
}

export async function mockPerformLifecycleAction(_action: any): Promise<any> {
  await delay()
  return {
    success: true,
    resourceId: _action.resourceId,
  }
}

export async function mockGetOperationsStats(): Promise<any> {
  await delay()
  return {
    activeSeasons: 2,
    upcomingEvents: 8,
    totalParticipants: 245789,
    rewardPoolRemaining: 5430000,
  }
}

export async function mockGetSeason(seasonId: string): Promise<any> {
  await delay()
  return {
    id: seasonId,
    name: 'Current Season',
    description: 'An exciting season with fresh challenges',
    status: 'active',
    number: 5,
    startDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
    endDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
    rewardPool: 500000,
    pointsMultiplier: 1.5,
    createdAt: new Date(Date.now() - 60 * 24 * 60 * 60 * 1000).toISOString(),
    createdBy: 'admin@synaptix.local',
    startedAt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
  }
}

export async function mockGetEvent(eventId: string): Promise<any> {
  await delay()
  return {
    id: eventId,
    name: 'Championship Tournament',
    description: 'Compete for glory and great rewards',
    type: 'tournament',
    status: 'active',
    startDate: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
    endDate: new Date(Date.now() + 2 * 24 * 60 * 60 * 1000).toISOString(),
    reward: 50000,
    participantCount: 2348,
    maxParticipants: 5000,
    createdAt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
    createdBy: 'admin@synaptix.local',
    openedAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
  }
}

// ============ Store Mock API ============

const PRODUCT_NAMES = ['Power Boost', 'Speed Potion', 'Shield Amulet', 'Wisdom Gem', 'Lucky Coin', 'Phoenix Feather', 'Dragon Scale', 'Mithril Ore']
const CATEGORIES = ['Consumables', 'Equipment', 'Collectibles', 'Boosts', 'Cosmetics']
const RARITIES: any[] = ['common', 'uncommon', 'rare', 'epic', 'legendary']

export async function mockGetProducts(_offset: number = 0, limit: number = 50): Promise<any> {
  await delay()
  const products = []
  for (let i = 0; i < limit; i++) {
    products.push({
      id: `product_${i}`,
      name: PRODUCT_NAMES[Math.floor(Math.random() * PRODUCT_NAMES.length)],
      description: 'A powerful item with unique abilities',
      price: Math.floor(Math.random() * 50000) + 1000,
      category: CATEGORIES[Math.floor(Math.random() * CATEGORIES.length)],
      rarity: RARITIES[Math.floor(Math.random() * RARITIES.length)],
      stock: Math.floor(Math.random() * 10000),
      maxStock: 10000,
      active: Math.random() > 0.1,
      createdAt: new Date(Date.now() - Math.random() * 90 * 24 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date(Date.now() - Math.random() * 7 * 24 * 60 * 60 * 1000).toISOString(),
    })
  }
  return { items: products, total: 500, offset: _offset, limit }
}

export async function mockCreateProduct(product: any): Promise<any> {
  await delay()
  return { id: `product_${Date.now()}`, ...product, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() }
}

export async function mockUpdateProduct(_id: string, product: any): Promise<any> {
  await delay()
  return { id: _id, ...product, updatedAt: new Date().toISOString() }
}

export async function mockDeleteProduct(_id: string): Promise<any> {
  await delay()
  return { success: true }
}

export async function mockGetFlashSales(_offset: number = 0, limit: number = 50): Promise<any> {
  await delay()
  const sales = []
  for (let i = 0; i < limit; i++) {
    const originalPrice = Math.floor(Math.random() * 50000) + 5000
    const discount = Math.floor(Math.random() * 70) + 10
    sales.push({
      id: `sale_${i}`,
      name: `Flash Sale ${i + 1}`,
      productId: `product_${Math.floor(Math.random() * 100)}`,
      productName: PRODUCT_NAMES[Math.floor(Math.random() * PRODUCT_NAMES.length)],
      discountPercentage: discount,
      originalPrice,
      salePrice: Math.floor(originalPrice * (1 - discount / 100)),
      startTime: new Date(Date.now() - Math.random() * 7 * 24 * 60 * 60 * 1000).toISOString(),
      endTime: new Date(Date.now() + Math.random() * 7 * 24 * 60 * 60 * 1000).toISOString(),
      maxUnits: Math.floor(Math.random() * 1000) + 100,
      unitsSold: Math.floor(Math.random() * 500),
      status: ['scheduled', 'active', 'ended'][Math.floor(Math.random() * 3)],
      createdAt: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString(),
    })
  }
  return { items: sales, total: 200, offset: _offset, limit }
}

export async function mockCreateFlashSale(sale: any): Promise<any> {
  await delay()
  return { id: `sale_${Date.now()}`, ...sale, createdAt: new Date().toISOString() }
}

export async function mockUpdateFlashSale(_id: string, sale: any): Promise<any> {
  await delay()
  return { id: _id, ...sale }
}

export async function mockDeleteFlashSale(_id: string): Promise<any> {
  await delay()
  return { success: true }
}

export async function mockGetStockPolicies(_offset: number = 0, limit: number = 50): Promise<any> {
  await delay()
  const policies = []
  for (let i = 0; i < limit; i++) {
    policies.push({
      id: `policy_${i}`,
      name: `Stock Policy ${i + 1}`,
      description: 'Manages inventory levels and reordering',
      reorderLevel: Math.floor(Math.random() * 500) + 50,
      reorderQuantity: Math.floor(Math.random() * 1000) + 500,
      maxStockLevel: Math.floor(Math.random() * 10000) + 5000,
      autoReorder: Math.random() > 0.3,
      createdAt: new Date(Date.now() - Math.random() * 90 * 24 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date(Date.now() - Math.random() * 7 * 24 * 60 * 60 * 1000).toISOString(),
    })
  }
  return { items: policies, total: 100, offset: _offset, limit }
}

export async function mockCreateStockPolicy(policy: any): Promise<any> {
  await delay()
  return { id: `policy_${Date.now()}`, ...policy, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() }
}

export async function mockUpdateStockPolicy(_id: string, policy: any): Promise<any> {
  await delay()
  return { id: _id, ...policy, updatedAt: new Date().toISOString() }
}

export async function mockDeleteStockPolicy(_id: string): Promise<any> {
  await delay()
  return { success: true }
}

export async function mockGetRewardLimits(_offset: number = 0, limit: number = 50): Promise<any> {
  await delay()
  const limits = []
  for (let i = 0; i < limit; i++) {
    const maxAmount = Math.floor(Math.random() * 100000) + 10000
    limits.push({
      id: `limit_${i}`,
      name: `Reward Limit ${i + 1}`,
      type: ['daily', 'weekly', 'seasonal'][Math.floor(Math.random() * 3)],
      maxAmount,
      currentAmount: Math.floor(Math.random() * maxAmount),
      resetDate: new Date(Date.now() + Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString(),
      status: Math.random() > 0.1 ? 'active' : 'paused',
      createdAt: new Date(Date.now() - Math.random() * 90 * 24 * 60 * 60 * 1000).toISOString(),
    })
  }
  return { items: limits, total: 50, offset: _offset, limit }
}

export async function mockCreateRewardLimit(limit: any): Promise<any> {
  await delay()
  return { id: `limit_${Date.now()}`, ...limit, createdAt: new Date().toISOString() }
}

export async function mockUpdateRewardLimit(_id: string, limit: any): Promise<any> {
  await delay()
  return { id: _id, ...limit }
}

export async function mockDeleteRewardLimit(_id: string): Promise<any> {
  await delay()
  return { success: true }
}

export async function mockGetStoreStats(): Promise<any> {
  await delay()
  return {
    totalProducts: 847,
    activeFlashSales: 12,
    totalRevenue: 5230000,
    lowStockCount: 34,
  }
}


// ============ Moderation Logs Mock API ============

const MOCK_MODERATION_STATUSES = ['normal', 'suspected', 'restricted', 'banned'] as const

function generateMockModerationLog(i: number) {
  return {
    id: `modlog-${i}`,
    playerId: `player-${(i % 5) + 1}`,
    newStatus: MOCK_MODERATION_STATUSES[i % 4],
    reason: ['Chat abuse', 'Suspicious win-rate', 'Chargeback', 'Appeal approved'][i % 4],
    notes: i % 3 === 0 ? 'Escalated from anti-cheat queue' : null,
    setByAdmin: ['ops@synaptix.dev', 'admin@synaptix.dev'][i % 2],
    createdAt: new Date(Date.now() - i * 7200_000).toISOString(),
    expiresAt: i % 4 === 2 ? new Date(Date.now() + 86400_000).toISOString() : null,
    relatedFlagId: i % 2 === 0 ? `flag-${i}` : null,
  }
}

export async function mockGetModerationLogs(filters?: { playerId?: string; status?: string }, offset: number = 0, limit: number = 50): Promise<any> {
  await delay()
  let items = Array.from({ length: 40 }, (_, i) => generateMockModerationLog(i))
  if (filters?.playerId) items = items.filter((l) => l.playerId === filters.playerId)
  if (filters?.status) items = items.filter((l) => l.newStatus === filters.status)
  return { items: items.slice(offset, offset + limit), total: items.length, offset, limit }
}

export async function mockGetModerationLogDetail(logId: string): Promise<any> {
  await delay()
  return { ...generateMockModerationLog(3), id: logId }
}

// ============ Store Player Stock & Analytics Mock API ============

export async function mockGetPlayerStock(playerId: string): Promise<any> {
  await delay()
  const skus = ['energy-pack-small', 'energy-pack-large', 'powerup-bundle', 'season-ticket']
  return {
    playerId,
    items: skus.map((sku, i) => ({
      sku,
      quantityUsed: i * 2,
      maxQuantity: 10,
      remaining: 10 - i * 2,
      effectiveMaxQuantity: i === 1 ? 20 : null,
      lastResetAtUtc: i % 2 === 0 ? new Date(Date.now() - 86400_000).toISOString() : null,
      nextResetAtUtc: new Date(Date.now() + 86400_000).toISOString(),
      updatedAtUtc: new Date().toISOString(),
    })),
  }
}

export async function mockGetPurchaseAnalytics(): Promise<any> {
  await delay()
  return {
    from: null,
    to: null,
    totalPurchases: 1284,
    totalCoinsSpent: 96400,
    topSkus: [
      { sku: 'energy-pack-small', purchaseCount: 412 },
      { sku: 'powerup-bundle', purchaseCount: 305 },
      { sku: 'energy-pack-large', purchaseCount: 198 },
      { sku: 'season-ticket', purchaseCount: 77 },
    ],
  }
}

export async function mockGetStockResetAnalytics(offset: number = 0, limit: number = 25): Promise<any> {
  await delay()
  const items = Array.from({ length: limit }, (_, i) => ({
    playerId: `player-${offset + i + 1}`,
    sku: ['energy-pack-small', 'powerup-bundle'][i % 2],
    lastResetAt: new Date(Date.now() - (i + 1) * 3600_000).toISOString(),
    nextResetAt: new Date(Date.now() + 86400_000).toISOString(),
    quantityUsed: i % 5,
  }))
  return { items, total: 120, offset, limit }
}
