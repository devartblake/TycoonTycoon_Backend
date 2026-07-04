import { apiGet } from '@/lib/api-client'
import type { Match, MatchStats } from './types'

export async function getMatches(playerId?: string, limit: number = 50): Promise<Match[]> {
  const url = playerId ? '/admin/matches?player='+playerId+'&limit='+limit : '/admin/matches?limit='+limit
  return apiGet(url)
}

export async function getMatchStats(playerId: string): Promise<MatchStats> {
  return apiGet('/admin/matches/'+playerId+'/stats')
}

export async function getMatchReplays(playerId: string): Promise<any[]> {
  return apiGet('/admin/matches/'+playerId+'/replays')
}
