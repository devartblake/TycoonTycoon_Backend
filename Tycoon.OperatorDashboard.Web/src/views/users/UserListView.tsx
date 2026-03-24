'use client'

import { useCallback, useEffect, useState } from 'react'

// Next Imports
import { useRouter } from 'next/navigation'

// MUI Imports
import Chip from '@mui/material/Chip'

// Component Imports
import PageHeader from '@components/admin/PageHeader'
import SearchFilterBar from '@components/admin/SearchFilterBar'
import type { FilterDef } from '@components/admin/SearchFilterBar'
import DataTable from '@components/admin/DataTable'
import type { Column } from '@components/admin/DataTable'

// Service Imports
import { userService } from '@/lib/services/userService'

// Type Imports
import type { AdminUserListItem } from '@/lib/types/admin'

const filters: FilterDef[] = [
  {
    key: 'isBanned',
    label: 'Status',
    options: [
      { label: 'Banned', value: 'true' },
      { label: 'Active', value: 'false' }
    ]
  },
  {
    key: 'isVerified',
    label: 'Verified',
    options: [
      { label: 'Verified', value: 'true' },
      { label: 'Unverified', value: 'false' }
    ]
  }
]

const columns: Column<AdminUserListItem>[] = [
  {
    id: 'username',
    label: 'Username',
    sortable: true,
    render: row => row.username
  },
  {
    id: 'email',
    label: 'Email',
    sortable: true,
    render: row => row.email
  },
  {
    id: 'role',
    label: 'Role',
    width: 100,
    render: row => row.role
  },
  {
    id: 'status',
    label: 'Status',
    width: 120,
    render: row =>
      row.isBanned ? (
        <Chip label='Banned' size='small' color='error' variant='tonal' />
      ) : (
        <Chip label='Active' size='small' color='success' variant='tonal' />
      )
  },
  {
    id: 'isVerified',
    label: 'Verified',
    width: 100,
    align: 'center',
    render: row =>
      row.isVerified ? (
        <i className='ri-checkbox-circle-line text-success' />
      ) : (
        <i className='ri-close-circle-line text-textDisabled' />
      )
  },
  {
    id: 'totalGamesPlayed',
    label: 'Games',
    width: 80,
    align: 'right',
    sortable: true,
    render: row => row.totalGamesPlayed.toLocaleString()
  },
  {
    id: 'winRate',
    label: 'Win %',
    width: 80,
    align: 'right',
    sortable: true,
    render: row => `${(row.winRate * 100).toFixed(1)}%`
  },
  {
    id: 'createdAt',
    label: 'Joined',
    width: 120,
    sortable: true,
    render: row => new Date(row.createdAt).toLocaleDateString()
  }
]

const UserListView = () => {
  const router = useRouter()

  // State
  const [rows, setRows] = useState<AdminUserListItem[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)
  const [search, setSearch] = useState('')
  const [filterValues, setFilterValues] = useState<Record<string, string>>({})
  const [sortBy, setSortBy] = useState<string | undefined>()
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc')
  const [loading, setLoading] = useState(true)

  const fetchUsers = useCallback(async () => {
    setLoading(true)

    try {
      const res = await userService.list({
        q: search || undefined,
        isBanned: filterValues.isBanned ? filterValues.isBanned === 'true' : undefined,
        isVerified: filterValues.isVerified ? filterValues.isVerified === 'true' : undefined,
        page,
        pageSize,
        sortBy,
        sortOrder
      })

      setRows(res.items)
      setTotal(res.totalItems)
    } catch {
      // API error — keep current state
    } finally {
      setLoading(false)
    }
  }, [search, filterValues, page, pageSize, sortBy, sortOrder])

  useEffect(() => {
    fetchUsers()
  }, [fetchUsers])

  const handleSearchChange = useCallback((value: string) => {
    setSearch(value)
    setPage(1)
  }, [])

  const handleFilterChange = useCallback((key: string, value: string) => {
    setFilterValues(prev => ({ ...prev, [key]: value }))
    setPage(1)
  }, [])

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
    (row: AdminUserListItem) => {
      router.push(`/users/${row.id}`)
    },
    [router]
  )

  return (
    <>
      <PageHeader title='Users' />
      <SearchFilterBar
        searchPlaceholder='Search by username or email...'
        searchValue={search}
        onSearchChange={handleSearchChange}
        filters={filters}
        filterValues={filterValues}
        onFilterChange={handleFilterChange}
      />
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
        emptyMessage='No users found'
      />
    </>
  )
}

export default UserListView
