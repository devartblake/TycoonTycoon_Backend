/**
 * useAuditEvents hook - manage security audit data and filtering
 */

import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import * as api from '../api'
import type { AuditEvent, AuditFilter, IPLocationData } from '../types'

export function useAuditEvents(filters?: AuditFilter, offset: number = 0, limit: number = 50) {
  return useQuery({
    queryKey: ['auditEvents', filters, offset, limit],
    queryFn: () => api.getAuditEvents(filters, offset, limit),
    staleTime: 1000 * 60 * 2, // 2 minutes
  })
}

export function useAuditStats() {
  return useQuery({
    queryKey: ['auditStats'],
    queryFn: () => api.getAuditStats(),
    staleTime: 1000 * 60 * 5, // 5 minutes
  })
}

export function useIPLocations(filters?: AuditFilter) {
  return useQuery({
    queryKey: ['ipLocations', filters],
    queryFn: () => api.getIPLocations(filters),
    staleTime: 1000 * 60 * 5, // 5 minutes
  })
}

export function useEventDetail(eventId: string) {
  return useQuery({
    queryKey: ['auditEvent', eventId],
    queryFn: () => api.getEventDetail(eventId),
    enabled: !!eventId,
    staleTime: 1000 * 60 * 10, // 10 minutes
  })
}

// Helper hook to aggregate location data from events for map clustering
export function useEventLocations(events: AuditEvent[] | undefined): IPLocationData[] {
  return useMemo(() => {
    if (!events) return []

    const locationMap = new Map<string, IPLocationData>()

    events.forEach((event) => {
      const key = `${event.country}-${event.city}`
      if (event.latitude && event.longitude) {
        if (locationMap.has(key)) {
          const existing = locationMap.get(key)!
          existing.eventCount += 1
        } else {
          locationMap.set(key, {
            ip: event.ipAddress,
            country: event.country || 'Unknown',
            city: event.city || 'Unknown',
            latitude: event.latitude,
            longitude: event.longitude,
            eventCount: 1,
          })
        }
      }
    })

    return Array.from(locationMap.values())
  }, [events])
}
