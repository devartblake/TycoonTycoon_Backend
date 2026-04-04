import { apiGetJson } from '../lib/apiClient'

export function getAuditLog({ page = 1, pageSize = 20, status = '' } = {}) {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize)
  })
  if (status) params.set('status', status)
  return apiGetJson(`/api/audit-log?${params.toString()}`)
}
