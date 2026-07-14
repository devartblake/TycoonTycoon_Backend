/**
 * Personalization API — Django admin_personalization_client + AdminPersonalizationEndpoints.
 *
 *   GET  /admin/personalization/summary
 *   GET  /admin/personalization/archetypes          → [{ archetype, count }]
 *   GET  /admin/personalization/recommendations/performance
 *   GET  /admin/personalization/player/{playerId}
 *   GET  /admin/personalization/debug/{playerId}
 *   POST /admin/personalization/player/{playerId}/recalculate
 *   POST /admin/personalization/player/{playerId}/reset
 *   GET  /admin/personalization/rules
 *   PUT  /admin/personalization/rules/{ruleKey}
 *   PUT  /admin/personalization/rules               (bulk)
 *
 * There are no recommendation-engines / recommendation-controls / stats CRUD
 * routes. Those UI concepts map onto performance + rules + summary below.
 */

import { apiGet, apiPost, apiPut } from '@/lib/api-client'
import type {
  PlayerArchetype,
  RecommendationEngine,
  RecommendationControl,
  PersonalizationStats,
  ArchetypesListResponse,
  RecommendationEnginesListResponse,
  RecommendationControlsListResponse,
} from './types'

interface BackendArchetypeCount {
  archetype?: string
  Archetype?: string
  count?: number
  Count?: number
}

interface BackendPerfRow {
  type?: string
  Type?: string
  total?: number
  Total?: number
  accepted?: number
  Accepted?: number
  acceptanceRate?: number
  AcceptanceRate?: number
  dismissalRate?: number
  DismissalRate?: number
}

interface BackendRuleDto {
  id?: string
  Id?: string
  ruleKey?: string
  RuleKey?: string
  description?: string
  Description?: string
  isEnabled?: boolean
  IsEnabled?: boolean
  rule?: unknown
  Rule?: unknown
  updatedAt?: string
  UpdatedAt?: string
}

interface BackendSummary {
  archetypeCounts?: Record<string, number>
  ArchetypeCounts?: Record<string, number>
  highChurnRiskCount?: number
  HighChurnRiskCount?: number
  highFrustrationRiskCount?: number
  HighFrustrationRiskCount?: number
  totalProfiles?: number
  TotalProfiles?: number
  generatedAt?: string
}

function archetypeName(row: BackendArchetypeCount): string {
  return row.archetype ?? row.Archetype ?? 'unknown'
}

function archetypeCount(row: BackendArchetypeCount): number {
  return Number(row.count ?? row.Count ?? 0)
}

function toArchetype(row: BackendArchetypeCount, index: number): PlayerArchetype {
  const name = archetypeName(row)
  const now = new Date().toISOString()
  return {
    id: name || `archetype-${index}`,
    name,
    description: `Players classified as "${name}"`,
    icon: 'user',
    characteristics: [],
    preferredCategories: [],
    engagementLevel: 'regular',
    averageSessionLength: 0,
    playerCount: archetypeCount(row),
    conversionRate: 0,
    retentionRate: 0,
    createdAt: now,
    updatedAt: now,
  }
}

function toEngine(row: BackendPerfRow, index: number): RecommendationEngine {
  const type = row.type ?? row.Type ?? `type-${index}`
  const total = Number(row.total ?? row.Total ?? 0)
  const acceptance = Number(row.acceptanceRate ?? row.AcceptanceRate ?? 0)
  const now = new Date().toISOString()
  return {
    id: type,
    name: type,
    description: `Recommendation type "${type}" (${total} total)`,
    enabled: true,
    algorithm: 'hybrid',
    version: '1',
    accuracy: acceptance,
    coverage: 0,
    diversity: 0,
    diversityWeight: 0,
    recencyWeight: 0,
    popularityWeight: 0,
    personalizationWeight: 0,
    createdAt: now,
    updatedAt: now,
  }
}

function toControl(rule: BackendRuleDto): RecommendationControl {
  const key = rule.ruleKey ?? rule.RuleKey ?? rule.id ?? rule.Id ?? 'rule'
  const id = String(rule.id ?? rule.Id ?? key)
  const enabled = Boolean(rule.isEnabled ?? rule.IsEnabled ?? true)
  const updated = String(rule.updatedAt ?? rule.UpdatedAt ?? new Date().toISOString())
  return {
    id,
    playerId: '',
    archetypeId: '',
    recommendationEngineId: key,
    enabled,
    frequency: 'daily',
    maxRecommendations: 10,
    categoriesIncluded: [],
    categoriesExcluded: [],
    minQualityScore: 0,
    lastUpdatedAt: updated,
  }
}

export async function getArchetypes(offset: number = 0, limit: number = 50): Promise<ArchetypesListResponse> {
  const rows = await apiGet<BackendArchetypeCount[]>('/admin/personalization/archetypes')
  const all = (Array.isArray(rows) ? rows : []).map(toArchetype)
  const items = all.slice(offset, offset + limit)
  return { items, total: all.length, offset, limit }
}

export async function getArchetype(id: string): Promise<PlayerArchetype> {
  const list = await getArchetypes(0, 500)
  const found = list.items.find((a) => a.id === id || a.name === id)
  if (!found) throw new Error(`Archetype ${id} not found`)
  return found
}

export async function createArchetype(
  _archetype: Omit<
    PlayerArchetype,
    'id' | 'playerCount' | 'conversionRate' | 'retentionRate' | 'createdAt' | 'updatedAt'
  >
): Promise<PlayerArchetype> {
  void _archetype
  throw new Error('Archetypes are derived from player profiles; create is not supported.')
}

export async function updateArchetype(
  _id: string,
  _archetype: Partial<PlayerArchetype>
): Promise<PlayerArchetype> {
  void _id
  void _archetype
  throw new Error('Archetypes are derived from player profiles; update is not supported.')
}

export async function deleteArchetype(_id: string): Promise<{ success: boolean }> {
  void _id
  throw new Error('Archetypes are derived from player profiles; delete is not supported.')
}

/** Maps recommendation performance rows → engines list for the existing UI. */
export async function getRecommendationEngines(
  offset: number = 0,
  limit: number = 50
): Promise<RecommendationEnginesListResponse> {
  const rows = await apiGet<BackendPerfRow[]>('/admin/personalization/recommendations/performance')
  const all = (Array.isArray(rows) ? rows : []).map(toEngine)
  const items = all.slice(offset, offset + limit)
  return { items, total: all.length, offset, limit }
}

export async function getRecommendationEngine(id: string): Promise<RecommendationEngine> {
  const list = await getRecommendationEngines(0, 500)
  const found = list.items.find((e) => e.id === id)
  if (!found) throw new Error(`Engine ${id} not found`)
  return found
}

export async function updateRecommendationEngine(
  _id: string,
  _engine: Partial<RecommendationEngine>
): Promise<RecommendationEngine> {
  void _id
  void _engine
  throw new Error('Engines are performance aggregates; edit rules instead.')
}

/** Toggle maps to rule isEnabled when id is a rule key; otherwise no-op error. */
export async function toggleRecommendationEngine(
  id: string,
  enabled: boolean
): Promise<RecommendationEngine> {
  await apiPut(`/admin/personalization/rules/${encodeURIComponent(id)}`, {
    isEnabled: enabled,
  })
  return {
    id,
    name: id,
    description: '',
    enabled,
    algorithm: 'hybrid',
    version: '1',
    accuracy: 0,
    coverage: 0,
    diversity: 0,
    diversityWeight: 0,
    recencyWeight: 0,
    popularityWeight: 0,
    personalizationWeight: 0,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  }
}

/** Rules list → controls list for the existing UI. */
export async function getRecommendationControls(
  offset: number = 0,
  limit: number = 50
): Promise<RecommendationControlsListResponse> {
  const rules = await apiGet<BackendRuleDto[]>('/admin/personalization/rules')
  const all = (Array.isArray(rules) ? rules : []).map(toControl)
  const items = all.slice(offset, offset + limit)
  return { items, total: all.length, offset, limit }
}

export async function updateRecommendationControl(
  id: string,
  control: Partial<RecommendationControl>
): Promise<RecommendationControl> {
  const ruleKey = control.recommendationEngineId || id
  const dto = await apiPut<BackendRuleDto>(`/admin/personalization/rules/${encodeURIComponent(ruleKey)}`, {
    isEnabled: control.enabled,
  })
  return toControl(dto)
}

export async function getPersonalizationStats(): Promise<PersonalizationStats> {
  const summary = await apiGet<BackendSummary>('/admin/personalization/summary')
  const totalPlayers = Number(summary.totalProfiles ?? summary.TotalProfiles ?? 0)
  const archetypes = summary.archetypeCounts ?? summary.ArchetypeCounts ?? {}
  const totalArchetypes = Object.keys(archetypes).length
  let perf: BackendPerfRow[] = []
  try {
    perf = await apiGet<BackendPerfRow[]>('/admin/personalization/recommendations/performance')
  } catch {
    perf = []
  }
  const avgAcc =
    perf.length === 0
      ? 0
      : perf.reduce((s, r) => s + Number(r.acceptanceRate ?? r.AcceptanceRate ?? 0), 0) / perf.length
  return {
    totalArchetypes,
    totalPlayers,
    activeRecommendationEngines: perf.length,
    averageEngagementScore: 0,
    recommendationAccuracy: avgAcc,
    playerSatisfactionScore: 0,
  }
}

/**
 * Archetype recalculate is not a backend op. Prefer player-level recalculate.
 * If `id` looks like a GUID, call player recalculate; else refresh summary only.
 */
export async function recalculateArchetypeMetrics(
  archetypeId: string
): Promise<{ success: boolean; message: string }> {
  const guidLike =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(archetypeId)
  if (guidLike) {
    await apiPost(`/admin/personalization/player/${archetypeId}/recalculate`, {})
    return { success: true, message: 'Player profile recalculated' }
  }
  await apiGet('/admin/personalization/summary')
  return {
    success: true,
    message: 'Archetypes are aggregate counts; pass a playerId to recalculate a profile.',
  }
}

export async function resetRecommendationModel(
  engineId: string
): Promise<{ success: boolean; message: string }> {
  const guidLike =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(engineId)
  if (guidLike) {
    await apiPost(`/admin/personalization/player/${engineId}/reset`, {})
    return { success: true, message: 'Player personalization profile reset' }
  }
  throw new Error('Reset expects a playerId GUID (Django: POST .../player/{id}/reset).')
}

// ── First-class Django parity helpers ───────────────────────────────────────

export async function getPersonalizationSummary(): Promise<BackendSummary> {
  return apiGet('/admin/personalization/summary')
}

export async function getPlayerPersonalizationProfile(playerId: string): Promise<unknown> {
  return apiGet(`/admin/personalization/player/${playerId}`)
}

export async function getPlayerPersonalizationDebug(playerId: string): Promise<unknown> {
  return apiGet(`/admin/personalization/debug/${playerId}`)
}

export async function recalculatePlayer(playerId: string): Promise<unknown> {
  return apiPost(`/admin/personalization/player/${playerId}/recalculate`, {})
}

export async function resetPlayer(playerId: string): Promise<unknown> {
  return apiPost(`/admin/personalization/player/${playerId}/reset`, {})
}

export async function listPersonalizationRules(): Promise<BackendRuleDto[]> {
  return apiGet('/admin/personalization/rules')
}

export async function upsertPersonalizationRule(
  ruleKey: string,
  payload: { isEnabled?: boolean; rule?: unknown; description?: string }
): Promise<BackendRuleDto> {
  return apiPut(`/admin/personalization/rules/${encodeURIComponent(ruleKey)}`, payload)
}
