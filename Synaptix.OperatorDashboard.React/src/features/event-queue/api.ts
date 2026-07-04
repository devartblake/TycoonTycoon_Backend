import { apiGet, apiPost } from '@/lib/api-client'
import type { QueuedEvent, EventStats } from './types'

export async function getQueuedEvents(status?: string): Promise<QueuedEvent[]> {
  const url = status ? '/admin/event-queue?status='+status : '/admin/event-queue'
  return apiGet(url)
}

export async function getEventStats(): Promise<EventStats> {
  return apiGet('/admin/event-queue/stats')
}

export async function retryEvent(eventId: string): Promise<QueuedEvent> {
  return apiPost('/admin/event-queue/'+eventId+'/retry', {})
}

export async function clearFailedEvents(): Promise<{ deleted: number }> {
  return apiPost('/admin/event-queue/clear-failed', {})
}
