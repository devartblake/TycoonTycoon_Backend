export interface Skill {
  id: string
  name: string
  description: string
  category: string
  unlockLevel: number
  costCoins: number
  costDiamonds: number
  enabled: boolean
  totalEquipped: number
  createdAt: string
}

export interface SkillSeed {
  id: string
  skillId: string
  playerId: string
  seedType: 'common' | 'rare' | 'epic' | 'legendary'
  level: number
  experience: number
  equipped: boolean
  claimedAt: string
}

export interface SkillStats {
  totalSkills: number
  totalSeeds: number
  activeSeeds: number
  mostEquipped: string
}
