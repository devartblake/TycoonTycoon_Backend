import { apiGetJson } from '../lib/apiClient'

export function getUsers({ page = 1, pageSize = 20, query = '', isBanned = '' } = {}) {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize)
  })
  if (query) params.set('q', query)
  if (isBanned !== '') params.set('isBanned', isBanned)
  return apiGetJson(`/api/users?${params.toString()}`)
}
