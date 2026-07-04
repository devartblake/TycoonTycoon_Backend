/**
 * Personalization API client
 * Handles player archetypes, recommendation engines, and personalization controls
 */
import { apiGet, apiPost, apiPut, apiPatch, apiDelete } from '@/lib/api-client';
// ── Player Archetypes ────────────────────────────────────────────────────────
export async function getArchetypes(offset = 0, limit = 50) {
    return apiGet(`/admin/personalization/archetypes?offset=${offset}&limit=${limit}`);
}
export async function getArchetype(id) {
    return apiGet(`/admin/personalization/archetypes/${id}`);
}
export async function createArchetype(archetype) {
    return apiPost('/admin/personalization/archetypes', archetype);
}
export async function updateArchetype(id, archetype) {
    return apiPut(`/admin/personalization/archetypes/${id}`, archetype);
}
export async function deleteArchetype(id) {
    await apiDelete(`/admin/personalization/archetypes/${id}`);
    return { success: true };
}
// ── Recommendation Engines ───────────────────────────────────────────────────
export async function getRecommendationEngines(offset = 0, limit = 50) {
    return apiGet(`/admin/personalization/recommendation-engines?offset=${offset}&limit=${limit}`);
}
export async function getRecommendationEngine(id) {
    return apiGet(`/admin/personalization/recommendation-engines/${id}`);
}
export async function updateRecommendationEngine(id, engine) {
    return apiPut(`/admin/personalization/recommendation-engines/${id}`, engine);
}
export async function toggleRecommendationEngine(id, enabled) {
    return apiPatch(`/admin/personalization/recommendation-engines/${id}/toggle`, { enabled });
}
// ── Recommendation Controls ──────────────────────────────────────────────────
export async function getRecommendationControls(offset = 0, limit = 50) {
    return apiGet(`/admin/personalization/recommendation-controls?offset=${offset}&limit=${limit}`);
}
export async function updateRecommendationControl(id, control) {
    return apiPut(`/admin/personalization/recommendation-controls/${id}`, control);
}
// ── Personalization Stats ────────────────────────────────────────────────────
export async function getPersonalizationStats() {
    return apiGet('/admin/personalization/stats');
}
// ── Recalculation & Reset ────────────────────────────────────────────────────
export async function recalculateArchetypeMetrics(archetypeId) {
    return apiPost(`/admin/personalization/archetypes/${archetypeId}/recalculate`, {});
}
export async function resetRecommendationModel(engineId) {
    return apiPost(`/admin/personalization/recommendation-engines/${engineId}/reset`, {});
}
