import { apiGet, apiPut } from '@/lib/api-client'
import type { Skill, SkillSeed } from './types'

export async function getSkills(): Promise<Skill[]> {
  return apiGet('/admin/skills')
}

export async function getSkillSeeds(skillId?: string): Promise<SkillSeed[]> {
  const url = skillId ? `/admin/skills/seeds?skill=${skillId}` : '/admin/skills/seeds'
  return apiGet(url)
}

export async function getSkillStats(): Promise<any> {
  return apiGet('/admin/skills/stats')
}

export async function updateSkill(id: string, data: Partial<Skill>): Promise<Skill> {
  return apiPut(`/admin/skills/${id}`, data)
}
