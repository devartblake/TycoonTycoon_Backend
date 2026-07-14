/**
 * Smoke: R1/R2 admin API helpers call Django-aligned paths.
 * Mocks the shared api client; does not hit a network.
 * @vitest-environment node
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'

const calls: { method: string; url: string }[] = []

vi.mock('@/lib/api-client', () => ({
  apiGet: vi.fn(async (url: string) => {
    calls.push({ method: 'GET', url })
    return defaultGet(url)
  }),
  apiPost: vi.fn(async (url: string, body?: unknown) => {
    calls.push({ method: 'POST', url })
    return defaultPost(url, body)
  }),
  apiPut: vi.fn(async (url: string) => {
    calls.push({ method: 'PUT', url })
    return {}
  }),
  apiPatch: vi.fn(async (url: string) => {
    calls.push({ method: 'PATCH', url })
    return {}
  }),
  apiDelete: vi.fn(async (url: string) => {
    calls.push({ method: 'DELETE', url })
    return {}
  }),
}))

vi.mock('@/lib/api-config', () => ({
  getMockMode: () => false,
}))

vi.mock('@/lib/mock-api-client', () => ({}))

vi.mock('@/features/auth/store', () => ({
  useAuthStore: {
    getState: () => ({ accessToken: 'test-token' }),
  },
}))

function defaultGet(url: string): unknown {
  if (url.includes('/admin/users') && !url.includes('/activity') && url.includes('?')) {
    return { items: [], page: 1, pageSize: 50, totalItems: 0, totalPages: 0 }
  }
  if (url.match(/\/admin\/users\/[^?/]+$/)) {
    return {
      id: 'u1',
      username: 'player',
      email: 'p@x.com',
      status: 'active',
      role: 'player',
      ageGroup: 'adult',
      createdAt: new Date().toISOString(),
      lastActive: null,
      totalGamesPlayed: 0,
      totalPoints: 0,
      winRate: 0,
      isVerified: true,
      isBanned: false,
    }
  }
  if (url.includes('/admin/moderation/profile/')) {
    return {
      playerId: '00000000-0000-0000-0000-000000000001',
      status: 1,
      setAtUtc: new Date().toISOString(),
    }
  }
  if (url.includes('/admin/moderation/logs')) {
    return { page: 1, pageSize: 50, total: 0, items: [] }
  }
  if (url.includes('/admin/audit/security')) {
    return { items: [], page: 1, pageSize: 50, totalItems: 0, totalPages: 0 }
  }
  if (url.includes('/admin/notifications/templates')) return []
  if (url.includes('/admin/notifications/channels')) return []
  if (url.includes('/admin/notifications/scheduled')) {
    return { items: [], page: 1, pageSize: 50, totalItems: 0, totalPages: 0 }
  }
  if (url.includes('/admin/store/catalog')) {
    return { items: [], page: 1, pageSize: 50, totalItems: 0, totalPages: 0 }
  }
  if (url.includes('/admin/store/flash-sales')) return { sales: [] }
  if (url.includes('/admin/store/stock-policies')) return { policies: [] }
  if (url.includes('/admin/questions?')) {
    return { items: [], total: 0, page: 1, pageSize: 50 }
  }
  if (url.includes('/admin/storage/prefixes')) return { prefixes: [] }
  if (url.includes('/admin/storage/objects')) return { items: [] }
  if (url.includes('/admin/personalization/archetypes')) return []
  if (url.includes('/admin/personalization/recommendations/performance')) return []
  if (url.includes('/admin/personalization/rules')) return []
  if (url.includes('/admin/personalization/summary')) {
    return { totalProfiles: 0, archetypeCounts: {} }
  }
  if (url.includes('/admin/economy/history/')) {
    return { playerId: 'p', page: 1, pageSize: 50, total: 0, items: [] }
  }
  if (url.includes('/admin/economy/players/')) {
    return {
      playerId: 'p',
      email: '',
      handle: '',
      currentBalance: 0,
      totalEarned: 0,
      totalSpent: 0,
      totalRefunded: 0,
      lastTransactionAt: null,
      accountCreatedAt: new Date().toISOString(),
    }
  }
  if (url.includes('/admin/economy/stats')) {
    return {
      totalPlayers: 0,
      totalCurrency: 0,
      averageBalance: 0,
      largestBalance: 0,
      smallestBalance: 0,
    }
  }
  if (url.includes('/admin/users/') && url.includes('/activity')) {
    return { items: [], page: 1, pageSize: 30, totalItems: 0 }
  }
  return {}
}

function defaultPost(url: string, _body?: unknown): unknown {
  if (url.includes('/admin/moderation/set-status')) {
    return {
      playerId: '00000000-0000-0000-0000-000000000001',
      status: 4,
      setAtUtc: new Date().toISOString(),
      setByAdmin: 'op',
      reason: 'test',
    }
  }
  return {}
}

beforeEach(() => {
  calls.length = 0
  vi.clearAllMocks()
})

describe('R1 path smoke', () => {
  it('users list uses page/pageSize/q', async () => {
    const users = await import('@/features/users/api')
    await users.getUsers({ email: 'a@b.com', limit: 25, offset: 25 })
    const get = calls.find((c) => c.method === 'GET' && c.url.includes('/admin/users'))
    expect(get?.url).toMatch(/page=2/)
    expect(get?.url).toMatch(/pageSize=25/)
    expect(get?.url).toMatch(/q=/)
  })

  it('moderation ban uses set-status', async () => {
    const mod = await import('@/features/moderation/api')
    await mod.banPlayer('00000000-0000-0000-0000-000000000001', 'cheating')
    expect(calls.some((c) => c.method === 'POST' && c.url === '/admin/moderation/set-status')).toBe(
      true
    )
  })

  it('audit list hits security path', async () => {
    const audit = await import('@/features/audit/api')
    await audit.getAuditEvents(undefined, 0, 50)
    expect(calls.some((c) => c.url.startsWith('/admin/audit/security'))).toBe(true)
  })
})

describe('R2 path smoke', () => {
  it('notifications templates path', async () => {
    const n = await import('@/features/notifications/api')
    await n.getTemplates()
    expect(calls.some((c) => c.url === '/admin/notifications/templates')).toBe(true)
  })

  it('store flash sales uses showAll', async () => {
    const s = await import('@/features/store/api')
    await s.getFlashSales(0, 50)
    expect(calls.some((c) => c.url.includes('/admin/store/flash-sales') && c.url.includes('showAll'))).toBe(
      true
    )
  })

  it('economy history path', async () => {
    const e = await import('@/features/economy/api')
    await e.getPlayerTransactions('00000000-0000-0000-0000-000000000001')
    expect(calls.some((c) => c.url.includes('/admin/economy/history/'))).toBe(true)
  })

  it('questions list path', async () => {
    const q = await import('@/features/content/api')
    await q.getQuestions(undefined, 0, 50)
    expect(calls.some((c) => c.url.startsWith('/admin/questions'))).toBe(true)
  })

  it('storage root uses prefixes', async () => {
    const st = await import('@/features/storage/api')
    await st.getStorageFolder('/')
    expect(calls.some((c) => c.url.includes('/admin/storage/prefixes'))).toBe(true)
  })

  it('personalization uses archetypes + performance + rules', async () => {
    const p = await import('@/features/personalization/api')
    await p.getArchetypes()
    await p.getRecommendationEngines()
    await p.getRecommendationControls()
    expect(calls.some((c) => c.url.includes('/admin/personalization/archetypes'))).toBe(true)
    expect(calls.some((c) => c.url.includes('/admin/personalization/recommendations/performance'))).toBe(
      true
    )
    expect(calls.some((c) => c.url.includes('/admin/personalization/rules'))).toBe(true)
  })
})
