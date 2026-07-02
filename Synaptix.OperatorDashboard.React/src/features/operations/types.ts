/**
 * Operations feature types
 */

export type SeasonStatus = 'draft' | 'scheduled' | 'active' | 'ended'
export type EventStatus = 'draft' | 'upcoming' | 'active' | 'ended' | 'cancelled'

export interface Season {
  id: string
  name: string
  description: string
  status: SeasonStatus
  number: number
  startDate: string
  endDate: string
  rewardPool: number
  pointsMultiplier: number
  createdAt: string
  createdBy: string
  startedAt?: string
  endedAt?: string
}

export interface GameEvent {
  id: string
  name: string
  description: string
  type: 'tournament' | 'challenge' | 'promotion' | 'special'
  status: EventStatus
  startDate: string
  endDate: string
  reward: number
  participantCount: number
  maxParticipants: number
  createdAt: string
  createdBy: string
  openedAt?: string
  closedAt?: string
}

export interface SeasonsListResponse {
  items: Season[]
  total: number
  offset: number
  limit: number
}

export interface EventsListResponse {
  items: GameEvent[]
  total: number
  offset: number
  limit: number
}

export interface SeasonFilter {
  status?: SeasonStatus
  searchText?: string
}

export interface EventFilter {
  status?: EventStatus
  type?: GameEvent['type']
  searchText?: string
}

export interface LifecycleAction {
  resourceId: string
  action: 'start' | 'close' | 'cancel'
  notes?: string
}

export interface OperationsStats {
  activeSeasons: number
  upcomingEvents: number
  totalParticipants: number
  rewardPoolRemaining: number
}
