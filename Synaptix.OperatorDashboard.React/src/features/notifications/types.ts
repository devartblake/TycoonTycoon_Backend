/**
 * Notifications feature types
 */

export interface NotificationTemplate {
  id: string
  name: string
  subject?: string
  body: string
  channels: ('email' | 'push' | 'sms')[]
  variables: string[]
  createdAt: string
  updatedAt: string
}

export interface NotificationChannel {
  id: string
  type: 'email' | 'push' | 'sms'
  name: string
  enabled: boolean
  config: Record<string, unknown>
  createdAt: string
}

export interface ScheduledNotification {
  id: string
  templateId: string
  templateName: string
  scheduledFor: string
  targetCount: number
  status: 'pending' | 'in_progress' | 'completed' | 'failed'
  createdAt: string
}

export interface DeadLetterMessage {
  id: string
  templateId: string
  templateName: string
  channel: 'email' | 'push' | 'sms'
  recipient: string
  error: string
  attemptCount: number
  createdAt: string
  lastAttemptAt: string
}

export interface TestSendPayload {
  templateId: string
  channel: 'email' | 'push' | 'sms'
  recipient: string
  variables?: Record<string, string>
}

export interface CreateTemplatePayload {
  name: string
  subject?: string
  body: string
  channels: ('email' | 'push' | 'sms')[]
}
