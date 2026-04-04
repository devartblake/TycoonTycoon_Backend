import { apiGetJson } from '../lib/apiClient'

export function getAuditLog(page = 1, pageSize = 20) {
  return apiGetJson(`/api/audit-log?page=${page}&pageSize=${pageSize}`)
}
