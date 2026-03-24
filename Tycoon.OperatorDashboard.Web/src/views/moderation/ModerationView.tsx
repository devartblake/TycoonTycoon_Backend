'use client'

import { useCallback, useEffect, useState } from 'react'

// Next Imports
import { useRouter } from 'next/navigation'

// Component Imports
import PageHeader from '@components/admin/PageHeader'
import DataTable from '@components/admin/DataTable'
import type { Column } from '@components/admin/DataTable'
import StatusBadge from '@components/admin/StatusBadge'

// Service Imports
import { moderationService } from '@/lib/services/moderationService'

// Type Imports
import type { ModerationLogItem, ModerationStatus } from '@/lib/types/admin'

const columns: Column<ModerationLogItem>[] = [
  {
    id: 'playerId',
    label: 'Player',
    sortable: true,
    render: row => row.playerId
  },
  {
    id: 'newStatus',
    label: 'Status',
    width: 130,
    render: row => <StatusBadge status={row.newStatus as ModerationStatus} />
  },
  {
    id: 'reason',
    label: 'Reason',
    render: row => row.reason ?? '—'
  },
  {
    id: 'setByAdmin',
    label: 'Admin',
    width: 140,
    render: row => row.setByAdmin ?? '—'
  },
  {
    id: 'createdAtUtc',
    label: 'Date',
    width: 120,
    sortable: true,
    render: row => new Date(row.createdAtUtc).toLocaleDateString()
  }
]

const ModerationView = () => {
  const router = useRouter()

  // State
  const [rows, setRows] = useState<ModerationLogItem[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)
  const [sortBy, setSortBy] = useState<string | undefined>()
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc')
  const [loading, setLoading] = useState(true)

  const fetchLogs = useCallback(async () => {
    setLoading(true)

    try {
      const res = await moderationService.logs({
        page,
        pageSize
      })

      setRows(res.items)
      setTotal(res.totalItems)
    } catch {
      // API error — keep current state
    } finally {
      setLoading(false)
    }
  }, [page, pageSize])

  useEffect(() => {
    fetchLogs()
  }, [fetchLogs])

  const handleSortChange = useCallback(
    (field: string) => {
      if (sortBy === field) {
        setSortOrder(prev => (prev === 'asc' ? 'desc' : 'asc'))
      } else {
        setSortBy(field)
        setSortOrder('asc')
      }
    },
    [sortBy]
  )

  const handleRowClick = useCallback(
    (row: ModerationLogItem) => {
      router.push(`/users/${row.playerId}`)
    },
    [router]
  )

  return (
    <>
      <PageHeader title='Moderation' />
      <DataTable
        columns={columns}
        rows={rows}
        rowKey={row => row.id}
        loading={loading}
        page={page}
        pageSize={pageSize}
        total={total}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        sortBy={sortBy}
        sortOrder={sortOrder}
        onSortChange={handleSortChange}
        onRowClick={handleRowClick}
        emptyMessage='No moderation logs found'
      />
    </>
  )
}

export default ModerationView
