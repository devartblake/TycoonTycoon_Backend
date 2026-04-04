import { apiGetJson } from '../lib/apiClient'

export function getUsers(page = 1, pageSize = 20) {
  return apiGetJson(`/api/users?page=${page}&pageSize=${pageSize}`)
}
