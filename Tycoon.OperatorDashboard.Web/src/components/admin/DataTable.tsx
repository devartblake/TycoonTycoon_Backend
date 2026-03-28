'use client'

import { type ReactNode, useCallback } from 'react'

import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableContainer from '@mui/material/TableContainer'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import TablePagination from '@mui/material/TablePagination'
import TableSortLabel from '@mui/material/TableSortLabel'
import Paper from '@mui/material/Paper'
import LinearProgress from '@mui/material/LinearProgress'
import Typography from '@mui/material/Typography'

export interface Column<T> {
  id: string
  label: string
  sortable?: boolean
  width?: number | string
  align?: 'left' | 'center' | 'right'
  render: (row: T) => ReactNode
}

export interface DataTableProps<T> {
  columns: Column<T>[]
  rows: T[]
  rowKey: (row: T) => string
  loading?: boolean
  page: number
  pageSize: number
  total: number
  onPageChange: (page: number) => void
  onPageSizeChange?: (pageSize: number) => void
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
  onSortChange?: (field: string) => void
  onRowClick?: (row: T) => void
  emptyMessage?: string
}

function DataTable<T>({
  columns,
  rows,
  rowKey,
  loading = false,
  page,
  pageSize,
  total,
  onPageChange,
  onPageSizeChange,
  sortBy,
  sortOrder,
  onSortChange,
  onRowClick,
  emptyMessage = 'No data found'
}: DataTableProps<T>) {
  const handleChangePage = useCallback(
    (_: unknown, newPage: number) => {
      onPageChange(newPage + 1)
    },
    [onPageChange]
  )

  const handleChangeRowsPerPage = useCallback(
    (event: React.ChangeEvent<HTMLInputElement>) => {
      onPageSizeChange?.(parseInt(event.target.value, 10))
      onPageChange(1)
    },
    [onPageSizeChange, onPageChange]
  )

  return (
    <Paper variant='outlined'>
      {loading && <LinearProgress />}
      <TableContainer>
        <Table size='small'>
          <TableHead>
            <TableRow>
              {columns.map(col => (
                <TableCell key={col.id} align={col.align ?? 'left'} sx={{ width: col.width }}>
                  {col.sortable && onSortChange ? (
                    <TableSortLabel
                      active={sortBy === col.id}
                      direction={sortBy === col.id ? sortOrder : 'asc'}
                      onClick={() => onSortChange(col.id)}
                    >
                      {col.label}
                    </TableSortLabel>
                  ) : (
                    col.label
                  )}
                </TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.length === 0 && !loading ? (
              <TableRow>
                <TableCell colSpan={columns.length} align='center' sx={{ py: 4 }}>
                  <Typography color='text.secondary'>{emptyMessage}</Typography>
                </TableCell>
              </TableRow>
            ) : (
              rows.map(row => (
                <TableRow
                  key={rowKey(row)}
                  hover
                  sx={onRowClick ? { cursor: 'pointer' } : undefined}
                  onClick={() => onRowClick?.(row)}
                >
                  {columns.map(col => (
                    <TableCell key={col.id} align={col.align ?? 'left'}>
                      {col.render(row)}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>
      <TablePagination
        component='div'
        count={total}
        page={page - 1}
        rowsPerPage={pageSize}
        onPageChange={handleChangePage}
        onRowsPerPageChange={handleChangeRowsPerPage}
        rowsPerPageOptions={[10, 25, 50, 100]}
      />
    </Paper>
  )
}

export default DataTable
