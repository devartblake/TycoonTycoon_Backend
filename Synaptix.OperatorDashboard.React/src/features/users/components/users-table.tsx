/**
 * Users table with TanStack Table
 */

import React, { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useReactTable, getCoreRowModel, flexRender, ColumnDef } from '@tanstack/react-table'
import { formatDate } from '@/lib/utils'
import type { User } from '../types'

interface UsersTableProps {
  users: User[]
  isLoading: boolean
  onSelectionChange: (selectedIds: string[]) => void
}

export function UsersTable({ users, isLoading, onSelectionChange }: UsersTableProps) {
  const [rowSelection, setRowSelection] = useState({})
  const navigate = useNavigate()

  const columns = useMemo<ColumnDef<User>[]>(
    () => [
      {
        id: 'select',
        header: ({ table }) => (
          <input
            type="checkbox"
            checked={table.getIsAllRowsSelected()}
            ref={(el) => {
              if (el) el.indeterminate = table.getIsSomeRowsSelected()
            }}
            onChange={table.getToggleAllRowsSelectedHandler()}
            className="cursor-pointer"
            onClick={(e) => e.stopPropagation()}
          />
        ),
        cell: ({ row }) => (
          <input
            type="checkbox"
            checked={row.getIsSelected()}
            onChange={row.getToggleSelectedHandler()}
            className="cursor-pointer"
            onClick={(e) => e.stopPropagation()}
          />
        ),
        size: 50,
      },
      {
        accessorKey: 'email',
        header: 'Email',
        cell: ({ row }) => (
          <Link
            to={`/users/${row.original.id}`}
            className="text-accent hover:underline font-medium"
            onClick={(e) => e.stopPropagation()}
          >
            {row.original.email}
          </Link>
        ),
      },
      {
        accessorKey: 'status',
        header: 'Status',
        cell: (info) => {
          const status = info.getValue() as string
          const statusColor = {
            active: 'bg-status-healthy/10 text-status-healthy',
            suspended: 'bg-status-degraded/10 text-status-degraded',
            banned: 'bg-status-offline/10 text-status-offline',
            inactive: 'bg-status-unknown/10 text-status-unknown',
          }
          return (
            <span
              className={`inline-block px-2 py-1 rounded text-xs font-medium ${statusColor[status as keyof typeof statusColor] ?? ''}`}
            >
              {status}
            </span>
          )
        },
      },
      {
        accessorKey: 'createdAt',
        header: 'Created',
        cell: (info) => formatDate(info.getValue() as string),
      },
      {
        accessorKey: 'lastActiveAt',
        header: 'Last Active',
        cell: (info) => {
          const date = info.getValue() as string | null
          return date ? formatDate(date) : '—'
        },
      },
      {
        accessorKey: 'flaggedCount',
        header: 'Flags',
        cell: (info) => {
          const count = info.getValue() as number
          return count > 0 ? (
            <span className="inline-block px-2 py-1 rounded bg-status-offline/10 text-status-offline text-xs font-medium">
              {count}
            </span>
          ) : (
            '—'
          )
        },
        size: 60,
      },
      {
        id: 'actions',
        header: '',
        cell: ({ row }) => (
          <Link
            to={`/users/${row.original.id}`}
            className="text-xs text-accent hover:underline whitespace-nowrap"
            onClick={(e) => e.stopPropagation()}
          >
            View →
          </Link>
        ),
        size: 70,
      },
    ],
    []
  )

  const table = useReactTable({
    data: users,
    columns,
    state: { rowSelection },
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
    manualPagination: true,
  })

  // Notify parent of selection changes
  React.useEffect(() => {
    const selectedIds = table.getSelectedRowModel().rows.map((row) => row.original.id)
    onSelectionChange(selectedIds)
  }, [rowSelection, table, onSelectionChange])

  if (isLoading) {
    return (
      <div className="space-y-2">
        {[...Array(5)].map((_, i) => (
          <div key={i} className="h-12 bg-bg-secondary rounded animate-pulse" />
        ))}
      </div>
    )
  }

  if (users.length === 0) {
    return (
      <div className="text-center py-12 text-ink-secondary">
        <p className="text-lg">No users found</p>
        <p className="text-sm">Try adjusting your filters</p>
      </div>
    )
  }

  return (
    <div className="overflow-x-auto border border-panel-border rounded">
      <table className="w-full text-sm">
        <thead className="bg-bg-secondary border-b border-panel-border">
          {table.getHeaderGroups().map((headerGroup) => (
            <tr key={headerGroup.id}>
              {headerGroup.headers.map((header) => (
                <th
                  key={header.id}
                  className="px-4 py-3 text-left font-medium text-ink-primary"
                  style={{ width: header.getSize() }}
                >
                  {header.isPlaceholder ? null : flexRender(header.column.columnDef.header, header.getContext())}
                </th>
              ))}
            </tr>
          ))}
        </thead>
        <tbody>
          {table.getRowModel().rows.map((row) => (
            <tr
              key={row.id}
              className="border-b border-panel-border hover:bg-bg-secondary transition-colors cursor-pointer"
              onClick={() => navigate(`/users/${row.original.id}`)}
            >
              {row.getVisibleCells().map((cell) => (
                <td key={cell.id} className="px-4 py-3" style={{ width: cell.column.getSize() }}>
                  {flexRender(cell.column.columnDef.cell, cell.getContext())}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
