/**
 * Configuration API client
 * Handles feature flags, admin ACL, and system settings
 */

import { apiGet, apiPost, apiPut, apiPatch, apiDelete } from '@/lib/api-client'
import type {
  FeatureFlag,
  AdminACL,
  SystemConfig,
  ConfigStats,
  FeatureFlagsListResponse,
  AdminACLListResponse,
} from './types'

// ── Feature Flags ────────────────────────────────────────────────────────────

export async function getFeatureFlags(offset: number = 0, limit: number = 50): Promise<FeatureFlagsListResponse> {
  return apiGet(`/admin/config/feature-flags?offset=${offset}&limit=${limit}`)
}

export async function getFeatureFlag(id: string): Promise<FeatureFlag> {
  return apiGet(`/admin/config/feature-flags/${id}`)
}

export async function createFeatureFlag(flag: Omit<FeatureFlag, 'id' | 'createdAt' | 'updatedAt' | 'createdBy' | 'lastModifiedBy'>): Promise<FeatureFlag> {
  return apiPost('/admin/config/feature-flags', flag)
}

export async function updateFeatureFlag(id: string, flag: Partial<FeatureFlag>): Promise<FeatureFlag> {
  return apiPut(`/admin/config/feature-flags/${id}`, flag)
}

export async function toggleFeatureFlag(id: string, enabled: boolean): Promise<FeatureFlag> {
  return apiPatch(`/admin/config/feature-flags/${id}/toggle`, { enabled })
}

export async function deleteFeatureFlag(id: string): Promise<{ success: boolean }> {
  await apiDelete(`/admin/config/feature-flags/${id}`)
  return { success: true }
}

// ── Admin ACL ────────────────────────────────────────────────────────────────

export async function getAdminACL(offset: number = 0, limit: number = 50): Promise<AdminACLListResponse> {
  return apiGet(`/admin/config/admin-acl?offset=${offset}&limit=${limit}`)
}

export async function getAdminACLEntry(id: string): Promise<AdminACL> {
  return apiGet(`/admin/config/admin-acl/${id}`)
}

export async function createAdminACL(acl: Omit<AdminACL, 'id' | 'createdAt' | 'updatedAt'>): Promise<AdminACL> {
  return apiPost('/admin/config/admin-acl', acl)
}

export async function updateAdminACL(id: string, acl: Partial<AdminACL>): Promise<AdminACL> {
  return apiPut(`/admin/config/admin-acl/${id}`, acl)
}

export async function deleteAdminACL(id: string): Promise<{ success: boolean }> {
  await apiDelete(`/admin/config/admin-acl/${id}`)
  return { success: true }
}

// ── System Configuration ─────────────────────────────────────────────────────

export async function getSystemConfig(): Promise<SystemConfig> {
  return apiGet('/admin/config/system')
}

export async function updateSystemConfig(config: Partial<SystemConfig>): Promise<SystemConfig> {
  return apiPut('/admin/config/system', config)
}

export async function setMaintenanceMode(enabled: boolean, message?: string): Promise<{ success: boolean }> {
  await apiPatch('/admin/config/system/maintenance', { enabled, message })
  return { success: true }
}

// ── Stats ────────────────────────────────────────────────────────────────────

export async function getConfigStats(): Promise<ConfigStats> {
  return apiGet('/admin/config/stats')
}
