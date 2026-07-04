import { apiGet, apiPut } from '@/lib/api-client';
export async function getSkills() {
    return apiGet('/admin/skills');
}
export async function getSkillSeeds(skillId) {
    const url = skillId ? `/admin/skills/seeds?skill=${skillId}` : '/admin/skills/seeds';
    return apiGet(url);
}
export async function getSkillStats() {
    return apiGet('/admin/skills/stats');
}
export async function updateSkill(id, data) {
    return apiPut(`/admin/skills/${id}`, data);
}
