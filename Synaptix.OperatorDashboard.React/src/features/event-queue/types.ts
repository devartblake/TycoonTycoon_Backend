export interface QueuedEvent {
  id: string
  type: string
  playerId?: string
  timestamp: string
  status: 'pending' | 'processing' | 'completed' | 'failed'
  data: Record<string, any>
  retryCount: number
  maxRetries: number
  error?: string
}

export interface EventStats {
  totalEvents: number
  pendingEvents: number
  processingEvents: number
  completedEvents: number
  failedEvents: number
  throughput: number
}
