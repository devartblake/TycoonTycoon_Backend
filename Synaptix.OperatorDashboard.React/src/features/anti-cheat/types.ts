/**
 * Anti-cheat feature types
 */

export interface AntiCheatFlag {
  id: string
  playerId: string
  playerEmail: string
  sessionId: string
  flagReason: string
  flagSeverity: 'low' | 'medium' | 'high' | 'critical'
  sessionTime: string
  telemetryData: {
    avgResponseTime: number
    responseTimeVariance: number
    accuracyRate: number
    suspiciousPatterns: string[]
  }
  status: 'pending' | 'reviewed' | 'appealed'
  createdAt: string
}

export interface QueueStats {
  pendingCount: number
  reviewedThisWeek: number
  completionRate: number
}

export interface VerdictPayload {
  flagId: string
  verdict: 'innocent' | 'suspicious' | 'confirmed'
  notes?: string
}

export interface VerdictResponse {
  success: boolean
  nextFlagId?: string
}
