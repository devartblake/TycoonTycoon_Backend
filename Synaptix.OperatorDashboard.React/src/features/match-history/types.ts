export interface Match {
  id: string
  playerId: string
  playerName: string
  opponentId: string
  opponentName: string
  startTime: string
  endTime: string
  duration: number
  playerScore: number
  opponentScore: number
  result: 'win' | 'loss' | 'draw'
  replay?: string
  recordingTime: string
}

export interface MatchStats {
  totalMatches: number
  wins: number
  losses: number
  winRate: number
  avgDuration: number
  totalPlayTime: number
}
