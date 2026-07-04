/**
 * Configuration feature types
 */

export interface FeatureFlag {
  id: string
  name: string
  key: string
  description: string
  enabled: boolean
  rolloutPercentage: number
  targetAudience: 'all' | 'beta' | 'internal' | 'custom'
  customRules?: string[]
  createdAt: string
  updatedAt: string
  createdBy: string
  lastModifiedBy: string
}

export interface AdminACL {
  id: string
  adminId: string
  adminEmail: string
  role: 'owner' | 'admin' | 'operator' | 'analyst'
  permissions: string[]
  restrictions?: {
    ipWhitelist?: string[]
    timeWindow?: { start: string; end: string }
    actionLimit?: number
  }
  createdAt: string
  updatedAt: string
}

export interface SystemConfig {
  maintenanceMode: boolean
  maintenanceMessage?: string
  analyticsEnabled: boolean
  debugLoggingEnabled: boolean
  rateLimitPerMinute: number
  maxConcurrentSessions: number
  sessionTimeoutMinutes: number
}

export interface ConfigStats {
  totalFeatureFlags: number
  enabledFlags: number
  disabledFlags: number
  totalAdmins: number
  activeAdmins: number
}

export interface FeatureFlagsListResponse {
  items: FeatureFlag[]
  total: number
  offset: number
  limit: number
}

export interface AdminACLListResponse {
  items: AdminACL[]
  total: number
  offset: number
  limit: number
}
