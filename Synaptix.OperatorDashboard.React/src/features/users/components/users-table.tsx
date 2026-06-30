/**
 * Users table with TanStack Table
 */

import React, { useState } from 'react'
import { useReactTable, getCoreRowModel, flexRender, ColumnDef } from '@tanstack/react-table'
import { formatDate } from '@/lib/utils'
import type { User } from '../types'

interface UsersTableProps {
  users: User[]
  isLoading: boolean
  onSelectionChange: (selectedIds: string[]) => void
}

const columns: ColumnDef<User>[] = [
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
      />
    ),
    cell: ({ row }) => (
      <input
        type="checkbox"
        checked={row.getIsSelected()}
        onChange={row.getToggleSelectedHandler()}
        className="cursor-pointer"
      />
    ),
    size: 50,
  },
  {
    accessorKey: 'email',
    header: 'Email',
    cell: (info) => <span className="text-accent hover:underline cursor-pointer">{info.getValue() as string}</span>,
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
        <span className={`inline-block px-2 py-1 rounded text-xs font-medium ${statusColor[status as keyof typeof statusColor]}`}>
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
]

export function UsersTable({ users, isLoading, onSelectionChange }: UsersTableProps) {
  const [rowSelection, setRowSelection] = useState({})

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
  }, [rowSelection, table])

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
            <tr key={row.id} className="border-b border-panel-border hover:bg-bg-secondary transition-colors">
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
