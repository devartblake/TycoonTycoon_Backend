/**
 * Personalization feature types
 */

export interface PlayerArchetype {
  id: string
  name: string
  description: string
  icon: string
  characteristics: string[]
  preferredCategories: string[]
  engagementLevel: 'casual' | 'regular' | 'hardcore'
  averageSessionLength: number
  playerCount: number
  conversionRate: number
  retentionRate: number
  createdAt: string
  updatedAt: string
}

export interface RecommendationEngine {
  id: string
  name: string
  description: string
  enabled: boolean
  algorithm: 'collaborative' | 'content-based' | 'hybrid' | 'ml'
  version: string
  accuracy: number
  coverage: number
  diversity: number
  diversityWeight: number
  recencyWeight: number
  popularityWeight: number
  personalizationWeight: number
  createdAt: string
  updatedAt: string
}

export interface RecommendationControl {
  id: string
  playerId: string
  archetypeId: string
  recommendationEngineId: string
  enabled: boolean
  frequency: 'hourly' | 'daily' | 'weekly'
  maxRecommendations: number
  categoriesIncluded: string[]
  categoriesExcluded: string[]
  minQualityScore: number
  lastUpdatedAt: string
}

export interface PersonalizationStats {
  totalArchetypes: number
  totalPlayers: number
  activeRecommendationEngines: number
  averageEngagementScore: number
  recommendationAccuracy: number
  playerSatisfactionScore: number
}

export interface ArchetypesListResponse {
  items: PlayerArchetype[]
  total: number
  offset: number
  limit: number
}

export interface RecommendationEnginesListResponse {
  items: RecommendationEngine[]
  total: number
  offset: number
  limit: number
}

export interface RecommendationControlsListResponse {
  items: RecommendationControl[]
  total: number
  offset: number
  limit: number
}
