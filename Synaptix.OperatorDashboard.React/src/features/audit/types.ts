/**
 * Audit feature types
 */

export interface AuditEvent {
  id: string
  eventType: 'login' | 'api_call' | 'permission_change' | 'data_export' | 'deletion' | 'configuration_change'
  adminEmail: string
  adminId: string
  resourceType: string
  resourceId: string
  action: string
  ipAddress: string
  userAgent: string
  country?: string
  city?: string
  latitude?: number
  longitude?: number
  status: 'success' | 'failure'
  failureReason?: string
  timestamp: string
  details?: Record<string, any>
}

export interface AuditEventListResponse {
  items: AuditEvent[]
  total: number
  offset: number
  limit: number
}

export interface AuditFilter {
  eventType?: AuditEvent['eventType']
  adminEmail?: string
  resourceType?: string
  status?: 'success' | 'failure'
  country?: string
  dateFrom?: string
  dateTo?: string
  searchText?: string
}

export interface IPLocationData {
  ip: string
  country: string
  city: string
  latitude: number
  longitude: number
  eventCount: number
}

export interface SecurityAuditStats {
  totalEvents: number
  successRate: number
  uniqueAdmins: number
  uniqueIPs: number
}
