/**
 * Personalization API client
 * Handles player archetypes, recommendation engines, and personalization controls
 */

import { apiGet, apiPost, apiPut, apiPatch, apiDelete } from '@/lib/api-client'
import type {
  PlayerArchetype,
  RecommendationEngine,
  RecommendationControl,
  PersonalizationStats,
  ArchetypesListResponse,
  RecommendationEnginesListResponse,
  RecommendationControlsListResponse,
} from './types'

// ── Player Archetypes ────────────────────────────────────────────────────────

export async function getArchetypes(offset: number = 0, limit: number = 50): Promise<ArchetypesListResponse> {
  return apiGet(`/admin/personalization/archetypes?offset=${offset}&limit=${limit}`)
}

export async function getArchetype(id: string): Promise<PlayerArchetype> {
  return apiGet(`/admin/personalization/archetypes/${id}`)
}

export async function createArchetype(archetype: Omit<PlayerArchetype, 'id' | 'playerCount' | 'conversionRate' | 'retentionRate' | 'createdAt' | 'updatedAt'>): Promise<PlayerArchetype> {
  return apiPost('/admin/personalization/archetypes', archetype)
}

export async function updateArchetype(id: string, archetype: Partial<PlayerArchetype>): Promise<PlayerArchetype> {
  return apiPut(`/admin/personalization/archetypes/${id}`, archetype)
}

export async function deleteArchetype(id: string): Promise<{ success: boolean }> {
  await apiDelete(`/admin/personalization/archetypes/${id}`)
  return { success: true }
}

// ── Recommendation Engines ───────────────────────────────────────────────────

export async function getRecommendationEngines(offset: number = 0, limit: number = 50): Promise<RecommendationEnginesListResponse> {
  return apiGet(`/admin/personalization/recommendation-engines?offset=${offset}&limit=${limit}`)
}

export async function getRecommendationEngine(id: string): Promise<RecommendationEngine> {
  return apiGet(`/admin/personalization/recommendation-engines/${id}`)
}

export async function updateRecommendationEngine(id: string, engine: Partial<RecommendationEngine>): Promise<RecommendationEngine> {
  return apiPut(`/admin/personalization/recommendation-engines/${id}`, engine)
}

export async function toggleRecommendationEngine(id: string, enabled: boolean): Promise<RecommendationEngine> {
  return apiPatch(`/admin/personalization/recommendation-engines/${id}/toggle`, { enabled })
}

// ── Recommendation Controls ──────────────────────────────────────────────────

export async function getRecommendationControls(offset: number = 0, limit: number = 50): Promise<RecommendationControlsListResponse> {
  return apiGet(`/admin/personalization/recommendation-controls?offset=${offset}&limit=${limit}`)
}

export async function updateRecommendationControl(id: string, control: Partial<RecommendationControl>): Promise<RecommendationControl> {
  return apiPut(`/admin/personalization/recommendation-controls/${id}`, control)
}

// ── Personalization Stats ────────────────────────────────────────────────────

export async function getPersonalizationStats(): Promise<PersonalizationStats> {
  return apiGet('/admin/personalization/stats')
}

// ── Recalculation & Reset ────────────────────────────────────────────────────

export async function recalculateArchetypeMetrics(archetypeId: string): Promise<{ success: boolean; message: string }> {
  return apiPost(`/admin/personalization/archetypes/${archetypeId}/recalculate`, {})
}

export async function resetRecommendationModel(engineId: string): Promise<{ success: boolean; message: string }> {
  return apiPost(`/admin/personalization/recommendation-engines/${engineId}/reset`, {})
}
