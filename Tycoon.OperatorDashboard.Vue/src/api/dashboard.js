import { apiGetJson } from '../lib/apiClient'

export function getDashboardOverview() {
  return apiGetJson('/api/dashboard/overview')
}
